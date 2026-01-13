using UnityEngine;

[ExecuteInEditMode]
public class GroupBillboard : MonoBehaviour
{
    // If true, the plant will only rotate around the Y axis
    [SerializeField] private bool lockVerticalRotation = true;

    void LateUpdate()
    {
        if (Camera.main == null) return;

        // Get the camera position
        Vector3 targetPos = Camera.main.transform.position;

        if (lockVerticalRotation)
        {
            // Set the target's height to the same as the object's height
            // This ensures the X-axis rotation stays at 0
            targetPos.y = transform.position.y;
        }

        // Calculate direction from object to adjusted camera position
        Vector3 direction = targetPos - transform.position;

        // Check if direction is not zero to avoid console warnings
        if (direction.sqrMagnitude > 0.001f)
        {
            // Create rotation looking at the target
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Apply rotation to the parent object
            transform.rotation = targetRotation;
        }
    }
}