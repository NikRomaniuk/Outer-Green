using UnityEngine;

public enum PotSize
{
    Small,
    Medium,
    Large
}

public class PotManager : MonoBehaviour
{
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
    [SerializeField] private Color boundsColor = Color.yellow;
    private Rect boundsZone;

    [SerializeField] private PlantSegmentsManager plantSegmentsManager;
    [SerializeField] private GameObject plantSlot;
    [SerializeField] private PotSize potSize = PotSize.Medium;

    void Start()
    {
        boundsZone = CreateBoundsZone();
    }

    void Update()
    {
        
    }

    /// <summary>
    /// Build a Rect from the Bounds parameters
    /// </summary>
    public Rect CreateBoundsZone()
    {
        float leftX = -Mathf.Abs(leftBound);
        float rightX = Mathf.Abs(rightBound);
        float bottomY = Mathf.Abs(yOffset);
        float topY = Mathf.Abs(topBound);

        float width = rightX - leftX;
        float height = topY - bottomY;
        return new Rect(leftX, bottomY, width, height);
    }

    // -=-=- Gizmo Drawing -=-=-

    /// <summary>
    /// Draws the boundsZone Rect (editor only)
    /// </summary>
    void OnDrawGizmos()
    {
        boundsZone = CreateBoundsZone();

        if (!drawBounds) return;

        // choose pivot: plantSlot center if available, otherwise this pot's transform
        Transform pivotTransform = (plantSlot != null) ? plantSlot.transform : transform;

        // Compute a rotation that faces the camera horizontally (ignore camera Y), similar to StaticBillboard
        Quaternion drawRotation;
        if (Camera.main != null)
        {
            Vector3 lookDir = Camera.main.transform.forward;
            lookDir.y = 0f; // keep rectangle vertical
            if (lookDir.sqrMagnitude > 0.0001f)
                drawRotation = Quaternion.LookRotation(lookDir);
            else
                drawRotation = pivotTransform.rotation; // fallback
        }
        else
        {
            drawRotation = pivotTransform.rotation;
        }

        // draw the bounds zone using local coordinates relative to the pivot, billboarded to camera
        Gizmos.color = boundsColor;
        DrawLocalRect(boundsZone, pivotTransform.position, drawRotation);
    }

    // Helper: draw a Rect defined in local coordinates
    // Uses pivotPos + rotation * localPoint so the rectangle can be billboarded to the camera
    private void DrawLocalRect(Rect r, Vector3 pivotPos, Quaternion rotation)
    {
        Vector3 localBL = new Vector3(r.xMin, r.yMin, 0f);
        Vector3 localBR = new Vector3(r.xMax, r.yMin, 0f);
        Vector3 localTL = new Vector3(r.xMin, r.yMax, 0f);
        Vector3 localTR = new Vector3(r.xMax, r.yMax, 0f);

        Vector3 bl = pivotPos + rotation * localBL;
        Vector3 br = pivotPos + rotation * localBR;
        Vector3 tl = pivotPos + rotation * localTL;
        Vector3 tr = pivotPos + rotation * localTR;

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
}
