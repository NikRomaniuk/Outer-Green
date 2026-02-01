using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════════════════════════
// PLANT MANAGER - Growth Orchestrator
// ═════════════════════════════════════════════════════════════════════════════════════════════════
// Orchestrates plant growth by coordinating with TimeManager and PlantSegmentsManager
//
// GROWTH ALGORITHM:
// 1. TimeManager fires OnCyclesPassed → HandleCycles() accumulates cycles
// 2. When accumulated cycles >= cyclesPerGrowth: Grow() is called
// 3. Grow() iterates all active Main slots and calls TrySpawnSegment()
// 4. New segments are registered in PlantSegmentsManager.nonFinishedSegments
// 5. TimeManager fires OnCyclesCompleted → FinishGrow() finalizes segments
// 6. FinalizeGrowth() registers new slots with PlantSegmentsManager for next cycle
//
// TWO-PHASE GROWTH:
// - Phase 1 (OnCyclesPassed): Logic processing - spawn segments, calculate positions
// - Phase 2 (OnCyclesCompleted): Visual finalization - register slots, update visuals
// This separation ensures all logic completes before next cycle begins
// ═════════════════════════════════════════════════════════════════════════════════════════════════

public class PlantManager : MonoBehaviour
{
    // -=-=- Configuration -=-=-
    [Header("References")]
    [SerializeField] private PotManager pot;
    [SerializeField] private SegmentSlot testSegmentSlot;
    [SerializeField] private PlantSegmentsManager segmentsManager;

    [Header("Growth Direction")]
    [Tooltip("Global growth direction for the plant")]
    [SerializeField] private Vector2 direction = Vector2.up;
    public Vector2 Direction => direction;
    
    [Header("Growth Settings")]
    [Tooltip("Number of cycles required for one growth iteration")]
    [SerializeField] private int cyclesPerGrowth = 2;

    // -=-=- State -=-=-
    private long _lastCycleGrown = -1; // sentinel: not yet initialized to a cycle index

    [Tooltip("Maximum random angle deviation in degrees")]
    [SerializeField, Range(0f, 90f)] private float maxRandomAngle = 15f;
    public float MaxRandomAngle => maxRandomAngle;

    [Header("Growth Bounds")]
    // yOffset: vertical distance from the object's pivot to the BOTTOM bound
    [Min(0)] public float yOffset = 5f;
    // topBound: vertical distance from the object's pivot to the TOP bound
    [Min(0)] public float topBound = 5f;
    // leftBound: horizontal distance from the object's pivot to the LEFT bound
    [Min(0)] public float leftBound = 5f;
    // rightBound: horizontal distance from the object's pivot to the RIGHT bound
    [Min(0)] public float rightBound = 5f;
    [SerializeField] private bool drawBounds = true;
    [SerializeField] private Color boundsColor = Color.blue;
    [SerializeField] private bool drawGrowthZone = true;
    [SerializeField] private Color growthZoneColor = Color.red;

    // -=-=- Cached Data -=-=-
    [HideInInspector] public Rect growthZone;
    private Rect boundsZone;

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    void Start()
    {
        growthZone = CreateGrowthZone();
        boundsZone = CreateBoundsZone();
    }

    private void OnEnable()
    {
        // Subscribe to the events when the object becomes active
        TimeManager.OnCyclesPassed += HandleCycles;
        Debug.Log($"[PlantManager] {gameObject.name} subscribed to TimeManager.OnCyclesPassed");
        TimeManager.OnCyclesCompleted += UpdatePlantVisuals;
        Debug.Log($"[PlantManager] {gameObject.name} subscribed to TimeManager.OnCyclesCompleted");
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        TimeManager.OnCyclesPassed -= HandleCycles;
        Debug.Log($"[PlantManager] {gameObject.name} unsubscribed from TimeManager.OnCyclesPassed");
        TimeManager.OnCyclesCompleted -= UpdatePlantVisuals;
        Debug.Log($"[PlantManager] {gameObject.name} unsubscribed from TimeManager.OnCyclesCompleted");
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // GROWTH SYSTEM - Event Handlers
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 1: Use the absolute cycle index to decide how many growth iterations to run.
    /// The parameter is treated as the current cycle index. `_lastCycleGrown` stores
    /// the cycle index when the plant last performed growth (initialized to the
    /// first received cycle to avoid retroactive growth).
    /// </summary>
    private void HandleCycles()
    {
        long currentCycle = TimeManager.Instance.CyclesTotal;

        Debug.Log($"[PlantManager] Received cycle index: {currentCycle}");

        // Initialize last grown to the current cycle on the first call
        if (_lastCycleGrown < 0)
        {
            _lastCycleGrown = currentCycle;
            Debug.Log($"[PlantManager] Initializing _lastCycleGrown to {currentCycle}");
            return;
        }

        long deltaCycles = currentCycle - _lastCycleGrown;
        long growthIterations = deltaCycles / cyclesPerGrowth;

        bool isCatchUp = growthIterations > 1;

        Debug.Log($"[PlantManager] deltaCycles: {deltaCycles}, growthIterations: {growthIterations}, isCatchUp: {isCatchUp}");

        if (growthIterations <= 0) return;

        if (isCatchUp)
        {
            SimulationManager.RegisterProcess();
            Debug.Log($"[PlantManager] Started simulating {growthIterations} growth iterations");
        }

        for (long i = 0; i < growthIterations; i++)
        {
            Grow(isCatchUp);
        }

        // advance last grown by the amount of cycles we consumed for growth
        _lastCycleGrown += growthIterations * cyclesPerGrowth;

        if (isCatchUp)
        {
            SimulationManager.UnregisterProcess();
            Debug.Log($"[PlantManager] Finished simulating {growthIterations} growth iterations");
        }
    }

    /// <summary>
    /// Phase 2: Finalize all spawned segments and register their slots
    /// </summary>
    private void UpdatePlantVisuals()
    {
        FinishGrow();
    }

    /// <summary>
    /// Growth iteration: spawn new segments from all active Main slots
    /// Segments are registered but not finalized (deferred to FinishGrow)
    /// </summary>
    private void Grow(bool isCatchUp)
    {
        if (segmentsManager == null)
        {
            Debug.LogWarning("[PlantManager] PlantSegmentsManager reference is missing!");
            return;
        }

        // Get all active slots from segments manager and create a copy to avoid collection modification during iteration
        var activeSlots = segmentsManager.GetActiveMainSlots();
        Debug.Log($"[PlantManager] Grow called, isCatchUp: {isCatchUp}, active slots: {activeSlots.Count}");

        // Create a copy of the list to iterate safely (new slots will be registered during spawn)
        var slotsCopy = new System.Collections.Generic.List<SegmentSlot>(activeSlots);

        // Attempt to spawn segments from each active Main slot
        int spawnAttempts = 0;
        foreach (var slot in slotsCopy)
        {
            if (slot != null && slot.State == SlotState.Alive && slot.Slot == SlotType.Main)
            {
                spawnAttempts++;
                bool success = slot.TrySpawnSegment(GetLocalGrowthVector(), isCatchUp);
                Debug.Log($"[PlantManager] Slot spawn attempt #{spawnAttempts}: {(success ? "SUCCESS" : "FAILED")}");
            }
        }
        Debug.Log($"[PlantManager] Total spawn attempts: {spawnAttempts}");
    }

    /// <summary>
    /// Finalize all non-finished segments: register their slots for next growth cycle
    /// This ensures slots are available only after their segment is fully processed
    /// </summary>
    private void FinishGrow()
    {
        if (segmentsManager == null)
        {
            Debug.LogWarning("[PlantManager] PlantSegmentsManager reference is missing!");
            return;
        }

        // Get all non-finished segments and finalize their growth
        var nonFinished = segmentsManager.nonFinishedSegments;
        Debug.Log($"[PlantManager] FinishGrow called, non-finished segments: {nonFinished.Count}");
        if (nonFinished.Count == 0) return;

        Debug.Log($"[PlantManager] FinishGrow: finalizing {nonFinished.Count} segments");

        // Iterate and finalize each segment
        for (int i = nonFinished.Count - 1; i >= 0; i--)
        {
            var segment = nonFinished[i];
            if (segment != null && !segment.isFinalized)
            {
                segment.FinalizeGrowth();
                Debug.Log($"[PlantManager] Finalized segment: {segment.gameObject.name}");
            }
        }

        // Clear the list after all segments are finalized
        nonFinished.Clear();
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // UTILITIES
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get the global growth direction transformed into local space
    /// This ensures rotation of the station doesn't affect growth logic
    /// </summary>
    /// <returns>Growth direction in local coordinates</returns>
    public Vector2 GetLocalGrowthVector()
    {
        // Transform world direction to local space
        Vector3 worldDir = direction.normalized;
        Vector3 localDir = transform.InverseTransformDirection(worldDir);
        return new Vector2(localDir.x, localDir.y).normalized;
    }

    /// <summary>
    /// Build a Rect from the Bounds parameters, clamped by the assigned Bounds from Pot
    /// </summary>
    public Rect CreateGrowthZone()
    {
        // compute local edge positions using the same semantics as the gizmo drawing
        // but clamp Plant values to Pot values when a Pot is assigned
        float useLeft = leftBound;
        float useRight = rightBound;
        float useYOffset = yOffset;
        float useTop = topBound;

        if (pot != null)
        {
            useLeft = Mathf.Min(useLeft, pot.leftBound);
            useRight = Mathf.Min(useRight, pot.rightBound);
            useYOffset = Mathf.Min(useYOffset, pot.yOffset);
            useTop = Mathf.Min(useTop, pot.topBound);
        }

        float leftX = -Mathf.Abs(useLeft);
        float rightX = Mathf.Abs(useRight);
        float bottomY = Mathf.Abs(useYOffset); // distance upward from pivot
        float topY = Mathf.Abs(useTop);

        float width = rightX - leftX;
        float height = topY - bottomY;
        return new Rect(leftX, bottomY, width, height);
    }

    /// <summary>
    /// Build a Rect from the Bounds parameters
    /// </summary>
    public Rect CreateBoundsZone()
    {
        // compute local edge positions using the same semantics as the gizmo drawing
        float leftX = -Mathf.Abs(leftBound);
        float rightX = Mathf.Abs(rightBound);
        float bottomY = Mathf.Abs(yOffset); // distance upward from pivot
        float topY = Mathf.Abs(topBound);

        float width = rightX - leftX;
        float height = topY - bottomY;
        return new Rect(leftX, bottomY, width, height);
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // EDITOR VISUALIZATION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Draws the boundsZone & growthZone Rects (editor only)
    /// </summary>
    void OnDrawGizmos()
    {
        // Recompute zones from current variables so Editor changes are immediately reflected
        growthZone = CreateGrowthZone();
        boundsZone = CreateBoundsZone();

        // draw bounds zone
        if (drawBounds)
        {
            Gizmos.color = boundsColor;
            DrawLocalRect(boundsZone, transform);
        }

        // draw growth zone
        if (drawGrowthZone)
        {
            Gizmos.color = growthZoneColor;
            DrawLocalRect(growthZone, transform);
        }
    }

    // Helper: draw a Rect defined in local coordinates (relative to pivot transform)
    private void DrawLocalRect(Rect r, Transform pivot)
    {
        Vector3 localBL = new Vector3(r.xMin, r.yMin, 0f);
        Vector3 localBR = new Vector3(r.xMax, r.yMin, 0f);
        Vector3 localTL = new Vector3(r.xMin, r.yMax, 0f);
        Vector3 localTR = new Vector3(r.xMax, r.yMax, 0f);

        Vector3 bl = pivot.TransformPoint(localBL);
        Vector3 br = pivot.TransformPoint(localBR);
        Vector3 tl = pivot.TransformPoint(localTL);
        Vector3 tr = pivot.TransformPoint(localTR);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
}
