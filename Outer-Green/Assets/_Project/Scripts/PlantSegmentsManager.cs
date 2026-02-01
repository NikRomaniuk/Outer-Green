using System.Collections.Generic;
using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════════════════════════
// PLANT SEGMENTS MANAGER - Central Registry
// ═════════════════════════════════════════════════════════════════════════════════════════════════
// Central registry and manager for all plant segments and their growth slots
//
// DATA STORED:
// - segments: All spawned segments (for tracking/cleanup)
// - nonFinishedSegments: Segments awaiting finalization (slot registration)
// - activeMainSlots: Main growth slots available for spawning
// - activeExtraSlots: Extra slots for branching (not yet used)
//
// WORKFLOW:
// 1. Initial slots/segments registered on Start()
// 2. During growth: PlantManager queries GetActiveMainSlots()
// 3. New segments register via RegisterSegment() → added to nonFinishedSegments
// 4. At cycle end: PlantManager calls FinalizeGrowth() on nonFinishedSegments
// 5. FinalizeGrowth() calls RegisterSlot() for each child slot
// 6. Newly registered slots become available for next growth cycle
//
// SLOT CLEANUP:
// - GetActiveMainSlots() removes null references and Dead slots
// - Ensures only valid, living slots are processed
// ═════════════════════════════════════════════════════════════════════════════════════════════════

public class PlantSegmentsManager : MonoBehaviour
{
    // -=-=- Configuration -=-=-
    [Header("Initial Slots")]
    [Tooltip("Initial segment slots to register at start")]
    [SerializeField] private List<SegmentSlot> initialSlots = new List<SegmentSlot>();

    [Header("Initial Segments")]
    [Tooltip("Initial segments to register at start")]
    [SerializeField] private List<PlantSegmentManager> initialSegments = new List<PlantSegmentManager>();

    // -=-=- Runtime Data -=-=-
    private List<SegmentSlot> slots = new List<SegmentSlot>();
    // Active slots available for spawning
    private List<SegmentSlot> activeMainSlots = new List<SegmentSlot>();
    private List<SegmentSlot> activeExtraSlots = new List<SegmentSlot>();
    
    // All segments and segments awaiting finalization
    public List<PlantSegmentManager> segments = new List<PlantSegmentManager>();
    public List<PlantSegmentManager> nonFinishedSegments = new List<PlantSegmentManager>();

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    void Start()
    {
        // Register initial slots
        if (initialSlots != null && initialSlots.Count > 0)
        {
            Debug.Log($"[PlantSegmentsManager] Registering {initialSlots.Count} initial slots");
            foreach (var slot in initialSlots)
            {
                if (slot != null)
                {
                    RegisterSlot(slot);
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlantSegmentsManager] No initial slots assigned in inspector!");
        }

        // Register initial segments
        if (initialSegments != null && initialSegments.Count > 0)
        {
            Debug.Log($"[PlantSegmentsManager] Registering {initialSegments.Count} initial segments");
            foreach (var segment in initialSegments)
            {
                if (segment != null)
                {
                    RegisterSegment(segment);
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlantSegmentsManager] No initial segments assigned in inspector!");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // SLOT REGISTRATION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Register a segment slot as active for growth
    /// </summary>
    public void RegisterSlot(SegmentSlot slot)
    {
        if (slot == null) return;

        if (!slots.Contains(slot))
        {
            slots.Add(slot);
        }

        if (slot.Slot == SlotType.Main && !activeMainSlots.Contains(slot))
        {
            activeMainSlots.Add(slot);
            Debug.Log($"[PlantSegmentsManager] Registered Main slot: {slot.gameObject.name}, total Main slots: {activeMainSlots.Count}");
        }
        else if (slot.Slot == SlotType.Extra && !activeExtraSlots.Contains(slot))
        {
            activeExtraSlots.Add(slot);
            Debug.Log($"[PlantSegmentsManager] Registered Extra slot: {slot.gameObject.name}, total Extra slots: {activeExtraSlots.Count}");
        }
    }

    /// <summary>
    /// Unregister a slot (when it dies)
    /// </summary>
    public void UnregisterActiveSlot(SegmentSlot slot)
    {
        if (slot == null) return;

        if (activeMainSlots.Contains(slot)) activeMainSlots.Remove(slot);
        if (activeExtraSlots.Contains(slot)) activeExtraSlots.Remove(slot);
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // SLOT QUERIES
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all active Main slots for growth iteration
    /// </summary>
    public IReadOnlyList<SegmentSlot> GetActiveMainSlots()
    {
        int beforeCleanup = activeMainSlots.Count;
        // Clean up any null references
        activeMainSlots.RemoveAll(slot => slot == null || slot.State == SlotState.Dead);
        int afterCleanup = activeMainSlots.Count;
        
        if (beforeCleanup != afterCleanup)
        {
            Debug.Log($"[PlantSegmentsManager] Cleaned up slots: {beforeCleanup} -> {afterCleanup}");
        }
        
        return activeMainSlots;
    }

    /// <summary>
    /// Get all active Extra slots
    /// </summary>
    public IReadOnlyList<SegmentSlot> GetActiveExtraSlots()
    {
        // Clean up any null references
        activeExtraSlots.RemoveAll(slot => slot == null || slot.State == SlotState.Dead);
        return activeExtraSlots;
    }

    /// <summary>
    /// Clear all registered slots
    /// </summary>
    public void ClearAllSlots()
    {
        activeMainSlots.Clear();
        activeExtraSlots.Clear();
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // SEGMENT REGISTRATION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Register a plant segment
    /// </summary>
    public void RegisterSegment(PlantSegmentManager segment)
    {
        if (segment == null) return;

        if (!segments.Contains(segment))
        {
            segments.Add(segment);
            Debug.Log($"[PlantSegmentsManager] Registered segment: {segment.gameObject.name}, total segments: {segments.Count}");
        }

        // Add to non-finished list if not yet finalized (for deferred slot registration)
        if (!segment.isFinalized && !nonFinishedSegments.Contains(segment))
        {
            nonFinishedSegments.Add(segment);
            Debug.Log($"[PlantSegmentsManager] Registered non-finished segment: {segment.gameObject.name}, total non-finished segments: {nonFinishedSegments.Count}");
        }
    }
}
