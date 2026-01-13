using UnityEngine;

[ExecuteInEditMode]
public class StaticBillboard : MonoBehaviour
{
    // Call this manually from the Inspector context menu if needed
    [ContextMenu("Align to Camera")]
    public void AlignToCamera()
    {
        if (Camera.main == null) return;

        // Get camera direction but ignore the height (Y)
        Vector3 lookDir = Camera.main.transform.forward;
        lookDir.y = 0; // Keep the plant vertical

        if (lookDir.sqrMagnitude > 0.001f)
        {
            // The plant faces the same direction as the camera's view vector
            // We use -lookDir if the sprites are facing the wrong way
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    void Start()
    {
        // Align once when the game starts
        AlignToCamera();
    }
}