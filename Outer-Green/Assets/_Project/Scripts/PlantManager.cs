using UnityEngine;

public class PlantManager : MonoBehaviour
{
    [Header("References")]
    // Current Pot Reference
    [SerializeField] private PotManager pot;

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

    private Rect growthZone;
    private Rect boundsZone;

    void Start()
    {
        growthZone = CreateGrowthZone();
        boundsZone = CreateBoundsZone();
    }

    void Update()
    {
        
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

    // -=-=- Gizmo Drawing -=-=-

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
