using System.Collections.Generic;
using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════════════════════════
// PLANT SEGMENT MANAGER - Individual Segment Controller
// ═════════════════════════════════════════════════════════════════════════════════════════════════
// Manages a single plant segment (leaf, stem, root, etc.)
//
// PURPOSE:
// - Holds references: plantManager, previousSegment, segmentCollider
// - Contains child SegmentSlots that spawn next generation of segments
// - Tracks finalization state
//
// FINALIZATION WORKFLOW:
// 1. Segment is spawned by SegmentSlot and registered as "non-finished"
// 2. At end of cycle, PlantManager calls FinalizeGrowth()
// 3. FinalizeGrowth() marks segment as finished
//
// This two-phase approach ensures slots don't spawn from incomplete segments
// ═════════════════════════════════════════════════════════════════════════════════════════════════

public class PlantSegmentManager : MonoBehaviour
{
    // -=-=- Configuration -=-=-
    [Header("References")]
    [Tooltip("Reference to the main plant manager")]
    public PlantManager plantManager;

    [Tooltip("Main collision source for this segment")]
    public PolygonCollider2D segmentCollider;

    [Tooltip("Reference to the previous segment in the chain")]
    public PlantSegmentManager previousSegment;

    [Header("Growth Vector")]
    [Tooltip("Local direction this segment grows towards (normalized)")]
    [SerializeField]
    private Vector2 growthVector = Vector2.up;
    public Vector2 GrowthVector => growthVector;

    [Header("Slots")]
    [SerializeField]
    private List<SegmentSlot> segmentSlots = new List<SegmentSlot>();

    public IReadOnlyList<SegmentSlot> SegmentSlots => segmentSlots;

    [Header("Visual Components")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private SpriteRenderer outlineRenderer;

    // -=-=- State -=-=-
    [HideInInspector] public bool isFinalized = false;

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // SLOT REGISTRATION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finalize growth
    /// Called by PlantManager.FinishGrow() at the end of growth cycle
    /// Slot registration is handled in SegmentSlot.SpawnSegment so this method only marks the segment finalized and enables visuals
    /// </summary>

    void Update()
    {
    }

    /// <summary>
    /// Finalize growth
    /// </summary>
    public void FinalizeGrowth()
    {
        if (isFinalized)
        {
            Debug.Log($"[PlantSegmentManager] {gameObject.name} already finalized, skipping");
            return;
        }

        Debug.Log($"[PlantSegmentManager] Finalizing growth for {gameObject.name}");
        // Slot registration moved to SegmentSlot.SpawnSegment so slots are available immediately after spawn
        // This method now only finalizes state and updates visuals
        Debug.Log($"[PlantSegmentManager] Finalization completed for {gameObject.name}");
        isFinalized = true;
        UpdateVisuals(isFinalized); // Enable visuals upon finalization
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // VISUAL MANAGEMENT
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Update visual components (enable/disable)
    /// </summary>
    public void UpdateVisuals(bool enable)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = enable;
        }
        if (outlineRenderer != null)
        {
            outlineRenderer.enabled = enable;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // EDITOR VISUALIZATION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw growth vector arrow in world space
        Gizmos.color = Color.green;
        Vector3 start = transform.position;
        
        // Transform local growth vector to world space
        Vector2 localDir = growthVector.normalized;
        Vector3 worldDir = transform.TransformDirection(new Vector3(localDir.x, localDir.y, 0f)).normalized;
        Vector3 end = start + worldDir * 0.5f;
        
        // Draw main arrow line
        Gizmos.DrawLine(start, end);
        
        // Draw arrowhead
        Vector2 rightLocal = RotateVector2(localDir, 135f) * 0.1f;
        Vector2 leftLocal = RotateVector2(localDir, -135f) * 0.1f;
        Vector3 rightWorld = transform.TransformDirection(new Vector3(rightLocal.x, rightLocal.y, 0f));
        Vector3 leftWorld = transform.TransformDirection(new Vector3(leftLocal.x, leftLocal.y, 0f));
        Gizmos.DrawLine(end, end + rightWorld);
        Gizmos.DrawLine(end, end + leftWorld);
    }
    
    /// <summary>
    /// Rotate a Vector2 by specified angle in degrees
    /// </summary>
    private static Vector2 RotateVector2(Vector2 v, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
#endif
}
