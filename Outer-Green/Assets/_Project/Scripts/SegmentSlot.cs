using System.Collections.Generic;
using UnityEngine;

public enum SlotType
{
    Main,    // Main growth slots - processed every growth cycle
    Extra    // Extra slots - for branching (not yet implemented)
}

public enum SlotState
{
    Alive,   // Can spawn segments
    Dead     // Already spawned or exhausted attempts
}

// ═════════════════════════════════════════════════════════════════════════════════════════════════
// SEGMENT SLOT - Spawn Point for New Segments
// ═════════════════════════════════════════════════════════════════════════════════════════════════
// Represents a growth point on a segment where new segments can spawn
//
// SPAWN ALGORITHM:
// 1. TrySpawnSegment() is called by PlantManager.Grow()
// 2. Iterate through sourcePool (prefabs) in random order
// 3. For each prefab, try maxRotationAttempts different rotations
// 4. CheckSpawnValidity() creates temporary collider to test for overlaps
// 5. If valid configuration found, SpawnSegment() instantiates the prefab
// 6. New segment registers itself with PlantSegmentsManager (deferred finalization)
// 7. Slot state changes to Dead
//
// COLLISION DETECTION:
// - Creates temporary GameObject hierarchy matching prefab structure
// - Copies PolygonCollider2D with exact transform to test position
// - Uses Physics2D.Overlap to check against existing segments
// - Excludes parent and grandparent segments to allow natural connection
//
// ROTATION COMPENSATION ALGORITHM:
// 1. Get slot's local direction and transform to world space
// 2. Apply random angle deviation within maxRandomAngle range
// 3. Blend randomized direction with plant's global direction using globalDirectionWeight
// 4. Calculate rotation so segment's local growthVector aligns with final blended direction
// Golden Rule: Think globally, apply locally
// ═════════════════════════════════════════════════════════════════════════════════════════════════

public class SegmentSlot : MonoBehaviour
{
    // -=-=- Configuration -=-=-
    [Header("Slot Settings")]
    [SerializeField]
    private SlotType slotType = SlotType.Main;
    public SlotType Slot => slotType;

    [SerializeField]
    private SlotState state = SlotState.Alive;
    public SlotState State => state;

    // -=-=- Growth Direction -=-=-
    [Header("Growth Direction")]
    [Tooltip("Local direction of growth from this slot (normalized)")]
    [SerializeField]
    private Vector2 direction = Vector2.up;
    public Vector2 Direction => direction;

    [Tooltip("Maximum random angle deviation in degrees")]
    [SerializeField, Range(0f, 90f)]
    private float maxRandomAngle = 15f;

    [Tooltip("Weight of global growth direction influence (0-1)")]
    [SerializeField, Range(0f, 1f)]
    private float globalDirectionWeight = 0.3f;

    // -=-=- Segment Spawning -=-=-
    [Header("Segment Spawning")]
    [Tooltip("Reference to the parent segment manager")]
    [SerializeField]
    private PlantSegmentManager parentSegment;
    public PlantSegmentManager ParentSegment => parentSegment;

    [Tooltip("Pool of prefabs to spawn from")]
    [SerializeField]
    private PlantSegmentManager[] sourcePool;

    [Tooltip("Own collider for collision detection")]
    [SerializeField]
    private Collider2D segmentCollider;

    // -=-=- Spawn Settings -=-=-
    [Header("Spawn Settings")]
    [Tooltip("Maximum rotation attempts per prefab")]
    [SerializeField]
    private int maxRotationAttempts = 10;

    [Tooltip("Layer mask for collision detection")]
    [SerializeField]
    private LayerMask collisionLayerMask = ~0;

    // -=-=- Cached Data (GC Optimization) -=-=-
    private readonly List<PlantSegmentManager> tempPool = new List<PlantSegmentManager>(16);
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(16);
    private ContactFilter2D contactFilter;
    private readonly List<Collider2D> excludeColliders = new List<Collider2D>(8);

    // -=-=- Spawned Segment Reference -=-=-
    // -=-=- State -=-=-
    private PlantSegmentManager spawnedSegment;
    public PlantSegmentManager SpawnedSegment => spawnedSegment;

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        // Initialize contact filter
        contactFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = collisionLayerMask
        };

        // Cache parent colliders for exclusion during overlap checks
        CacheExcludeColliders();
    }

    void Start()
    {
        // Auto-assign segment collider if not set
        if (segmentCollider == null)
        {
            segmentCollider = GetComponent<Collider2D>();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // COLLISION EXCLUSION SETUP
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cache all colliders from parent segment and its previous segment for exclusion during collision checks
    /// This allows new segments to naturally connect to their parent without collision rejection
    /// </summary>
    private void CacheExcludeColliders()
    {
        excludeColliders.Clear();
        if (parentSegment != null)
        {
            // Add parent segment collider
            if (parentSegment.segmentCollider != null)
            {
                excludeColliders.Add(parentSegment.segmentCollider);
            }

            // Add previous segment collider (if exists)
            if (parentSegment.previousSegment != null && parentSegment.previousSegment.segmentCollider != null)
            {
                excludeColliders.Add(parentSegment.previousSegment.segmentCollider);
            }
        }
    }

    /// <summary>
    /// Set the parent segment reference (used when spawning)
    /// </summary>
    public void SetParentSegment(PlantSegmentManager parent)
    {
        parentSegment = parent;
        CacheExcludeColliders();
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // DIRECTION CALCULATION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calculate final growth direction using the new rotation compensation algorithm
    /// 1. Transform slot's local direction to world space
    /// 2. Apply random angle deviation
    /// 3. Blend with plant's global growth direction using globalDirectionWeight
    /// 4. Return the target direction in world space (segment rotation will be calculated in spawn)
    /// </summary>
    /// <param name="globalGrowthDirection">Plant's global growth direction (world space)</param>
    /// <param name="segmentPrefab">The segment prefab being spawned</param>
    /// <returns>Target direction in world space for the segment to grow along</returns>
    public Vector2 GetFinalDirection(Vector2 globalGrowthDirection, PlantSegmentManager segmentPrefab)
    {
        // Step 1: Get base vector - slot's local direction transformed to world space
        Vector2 localDir = direction.normalized;
        Vector3 worldDir3D = transform.TransformDirection(new Vector3(localDir.x, localDir.y, 0f));
        Vector2 worldDir = new Vector2(worldDir3D.x, worldDir3D.y).normalized;

        // Step 2: Apply random deviation
        float randomAngle = Random.Range(-maxRandomAngle, maxRandomAngle);
        Vector2 randomizedDir = RotateVector2(worldDir, randomAngle);

        // Step 3: Blend with plant's global growth direction using weight
        // weight = 0: use only randomized slot direction
        // weight = 1: use only plant's global direction
        // weight = 0.5: halfway between both
        Vector2 finalDirection = Vector2.Lerp(randomizedDir, globalGrowthDirection.normalized, globalDirectionWeight);

        // Normalize and fallback if needed
        if (finalDirection.sqrMagnitude < 0.001f)
        {
            finalDirection = worldDir;
        }
        else
        {
            finalDirection.Normalize();
        }

        return finalDirection;
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

    /// <summary>
    /// Convert direction vector to rotation angle in degrees (Z-axis)
    /// </summary>
    private static float DirectionToAngle(Vector2 dir)
    {
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // SPAWN SYSTEM
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempt to spawn a segment from the source pool
    /// </summary>
    /// <param name="globalGrowthDirection">External growth direction influence</param>
    /// <param name="isCatchUp">If true, immediately finalize growth (catch-up simulation mode)</param>
    /// <returns>True if segment was successfully spawned</returns>
    public bool TrySpawnSegment(Vector2 globalGrowthDirection, bool isCatchUp = false)
    {
        Debug.Log($"[SegmentSlot] TrySpawnSegment called on {gameObject.name}, state: {state}, isCatchUp: {isCatchUp}");
        
        if (state == SlotState.Dead)
        {
            Debug.Log($"[SegmentSlot] {gameObject.name} is Dead, cannot spawn");
            return false;
        }

        if (sourcePool == null || sourcePool.Length == 0)
        {
            Debug.LogWarning($"[SegmentSlot] Source pool is empty on {gameObject.name}");
            state = SlotState.Dead;
            return false;
        }
        
        Debug.Log($"[SegmentSlot] {gameObject.name} has {sourcePool.Length} prefabs in pool");

        // Build temporary pool (reuse list to avoid GC)
        tempPool.Clear();
        for (int i = 0; i < sourcePool.Length; i++)
        {
            if (sourcePool[i] != null)
            {
                tempPool.Add(sourcePool[i]);
            }
        }

        while (tempPool.Count > 0)
        {
            // Pick random prefab from temp pool
            int prefabIndex = Random.Range(0, tempPool.Count);
            PlantSegmentManager prefab = tempPool[prefabIndex];

            // Try multiple rotation variants
            for (int rotationAttempt = 0; rotationAttempt < maxRotationAttempts; rotationAttempt++)
            {
                // Get target direction in world space (steps 1-3 of algorithm)
                Vector2 targetDirection = GetFinalDirection(globalGrowthDirection, prefab);
                
                // Step 4: Calculate rotation so segment's growthVector aligns with target direction
                // Get segment's local growth vector
                Vector2 segmentLocalGrowth = prefab.GrowthVector.normalized;
                
                // Calculate angle needed to rotate segment's local growth vector to target direction
                float segmentLocalAngle = Mathf.Atan2(segmentLocalGrowth.y, segmentLocalGrowth.x) * Mathf.Rad2Deg - 90f;
                float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg - 90f;
                float finalRotationAngle = targetAngle - segmentLocalAngle;
                
                Quaternion rotation = Quaternion.Euler(0f, 0f, finalRotationAngle);

                // Check if this configuration is valid (no collisions)
                if (CheckSpawnValidity(prefab, transform.position, rotation))
                {
                    Debug.Log($"[SegmentSlot] {gameObject.name} found valid spawn configuration, spawning {prefab.name}");
                    // Spawn the segment
                    spawnedSegment = SpawnSegment(prefab, transform.position, rotation, isCatchUp);
                    state = SlotState.Dead;
                    return true;
                }
            }

            // All rotation attempts failed for this prefab - remove from pool
            Debug.Log($"[SegmentSlot] {gameObject.name} failed all {maxRotationAttempts} rotation attempts for {prefab.name}, removing from pool");
            tempPool.RemoveAt(prefabIndex);
        }

        // All prefabs exhausted without valid spawn
        Debug.LogWarning($"[SegmentSlot] {gameObject.name} exhausted all prefabs without finding valid spawn");
        state = SlotState.Dead;
        return false;
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // COLLISION DETECTION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Check if spawning a segment at given position/rotation would cause collisions
    /// Creates temporary GameObject hierarchy with PolygonCollider2D to test exact overlap
    /// Accounts for collider's local transform relative to prefab root (pivot point)
    /// </summary>
    private bool CheckSpawnValidity(PlantSegmentManager prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return false;

        // Get prefab's polygon collider
        PolygonCollider2D prefabCollider = prefab.segmentCollider;
        if (prefabCollider == null)
        {
            // No collider = no collision issues, allow spawn
            return true;
        }

        // Create temporary root GameObject at spawn position (prefab's pivot point)
        GameObject tempRoot = new GameObject("TempCollisionCheckRoot");
        tempRoot.transform.position = position;
        tempRoot.transform.rotation = rotation;

        // Create child object to hold the collider at the same relative transform as in the prefab
        GameObject tempColliderGO = new GameObject("TempCollider");
        tempColliderGO.transform.SetParent(tempRoot.transform, false);
        tempColliderGO.layer = prefabCollider.gameObject.layer;
        
        // Match the local transform of the collider relative to prefab root
        Transform prefabColliderTransform = prefabCollider.transform;
        Transform prefabRootTransform = prefab.transform;
        
        tempColliderGO.transform.localPosition = prefabRootTransform.InverseTransformPoint(prefabColliderTransform.position);
        tempColliderGO.transform.localRotation = Quaternion.Inverse(prefabRootTransform.rotation) * prefabColliderTransform.rotation;
        tempColliderGO.transform.localScale = prefabColliderTransform.localScale;

        // Copy polygon collider configuration to temporary object
        PolygonCollider2D tempCollider = tempColliderGO.AddComponent<PolygonCollider2D>();
        tempCollider.isTrigger = prefabCollider.isTrigger;
        tempCollider.offset = prefabCollider.offset;
        
        // Copy all polygon paths from prefab
        tempCollider.pathCount = prefabCollider.pathCount;
        for (int pathIndex = 0; pathIndex < prefabCollider.pathCount; pathIndex++)
        {
            Vector2[] path = prefabCollider.GetPath(pathIndex);
            tempCollider.SetPath(pathIndex, path);
        }

        // Force physics update to ensure collider is properly initialized
        Physics2D.SyncTransforms();

        // Check for overlaps using the temporary collider
        overlapResults.Clear();
        int hitCount = tempCollider.Overlap(contactFilter, overlapResults);

        // Clean up temporary objects
        Destroy(tempRoot);

        // Filter out excluded colliders (parent and its previous segment) and own slot collider
        int validHits = 0;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = overlapResults[i];

            // Ignore own slot collider
            if (hit == segmentCollider)
            {
                continue;
            }

            // Ignore excluded colliders (parent segment and its previous segment)
            bool isExcluded = false;
            for (int j = 0; j < excludeColliders.Count; j++)
            {
                if (hit == excludeColliders[j])
                {
                    isExcluded = true;
                    break;
                }
            }

            if (!isExcluded)
            {
                validHits++;
            }
        }

        // Valid if no remaining collisions
        return validHits == 0;
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // SEGMENT INSTANTIATION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Instantiate the segment at the slot position with proper hierarchy
    /// </summary>
    private PlantSegmentManager SpawnSegment(PlantSegmentManager prefab, Vector3 position, Quaternion rotation, bool isCatchUp)
    {
        // Determine parent transform - same level as parentSegment
        Transform newParent = null;
        if (parentSegment != null && parentSegment.transform.parent != null)
        {
            newParent = parentSegment.transform.parent;
        }

        Vector3 localPosition;
        Quaternion localRotation;

        if (newParent != null)
        {
            // Calculate local position and rotation that will give us the desired world position/rotation
            localPosition = newParent.InverseTransformPoint(position);
            
            // Actually, for pure 2D we should just keep Z rotation in local space
            float worldZRotation = rotation.eulerAngles.z;
            localRotation = Quaternion.Euler(0f, 0f, worldZRotation);
            
            // Adjust local Z to compensate for parent's local position Z
            localPosition.z = 0f;
            
        }
        else
        {
            localPosition = position;
            localRotation = rotation;
        }

        // Instantiate with parent and calculated local transform
        PlantSegmentManager newSegment = Instantiate(prefab, newParent);
        newSegment.transform.localPosition = localPosition;
        newSegment.transform.localRotation = localRotation;
        
        // Reset finalization flag (prefab might have isFinalized=true)
        newSegment.isFinalized = false;
        
        // Set reference to previous segment
        newSegment.previousSegment = parentSegment;
        
        // Copy plant manager reference from parent
        if (parentSegment != null && parentSegment.plantManager != null)
        {
            newSegment.plantManager = parentSegment.plantManager;
        }

        // Initialize slots in the new segment to reference back to this segment
        IReadOnlyList<SegmentSlot> newSlots = newSegment.SegmentSlots;
        for (int i = 0; i < newSlots.Count; i++)
        {
            newSlots[i].SetParentSegment(newSegment);
        }

        // Register the segment with PlantSegmentsManager for deferred finalization
        // FinalizeGrowth will be called later by PlantManager.FinishGrow()
        if (newSegment.plantManager != null)
        {
            PlantSegmentsManager segmentsManager = newSegment.plantManager.GetComponent<PlantSegmentsManager>();
            if (segmentsManager != null)
            {
                segmentsManager.RegisterSegment(newSegment);
            }
        }

        return newSegment;
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // STATE MANAGEMENT
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Force the slot to Dead state
    /// </summary>
    public void Kill()
    {
        state = SlotState.Dead;
    }

    /// <summary>
    /// Reset slot to Alive state
    /// </summary>
    public void Revive()
    {
        state = SlotState.Alive;
        spawnedSegment = null;
    }

    // -=-=- Editor Helpers -=-=-
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw direction arrow in local space transformed to world space
        Gizmos.color = state == SlotState.Alive ? Color.green : Color.red;
        Vector3 start = transform.position;

        Vector2 localDir = direction.normalized;
        Vector3 worldDir = transform.TransformDirection(new Vector3(localDir.x, localDir.y, 0f)).normalized;
        Vector3 end = start + worldDir * 0.5f;
        Gizmos.DrawLine(start, end);

        // Draw arrowhead (rotate in local space then transform)
        Vector2 rightLocal = RotateVector2(localDir, 135f) * 0.1f;
        Vector2 leftLocal = RotateVector2(localDir, -135f) * 0.1f;
        Vector3 rightWorld = transform.TransformDirection(new Vector3(rightLocal.x, rightLocal.y, 0f));
        Vector3 leftWorld = transform.TransformDirection(new Vector3(leftLocal.x, leftLocal.y, 0f));
        Gizmos.DrawLine(end, end + rightWorld);
        Gizmos.DrawLine(end, end + leftWorld);

        // Draw random angle cone (local -> world)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector2 leftBoundLocal = RotateVector2(localDir, -maxRandomAngle) * 0.5f;
        Vector2 rightBoundLocal = RotateVector2(localDir, maxRandomAngle) * 0.5f;
        Vector3 leftBoundWorld = transform.TransformDirection(new Vector3(leftBoundLocal.x, leftBoundLocal.y, 0f));
        Vector3 rightBoundWorld = transform.TransformDirection(new Vector3(rightBoundLocal.x, rightBoundLocal.y, 0f));
        Gizmos.DrawLine(start, start + leftBoundWorld);
        Gizmos.DrawLine(start, start + rightBoundWorld);
    }
#endif
}
