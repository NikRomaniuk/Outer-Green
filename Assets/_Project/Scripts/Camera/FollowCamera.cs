using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 2f, -10f);
    [SerializeField] float smoothTime = 0.2f;
    [SerializeField] bool useBounds = false;
    [SerializeField] Vector2 minBounds = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    [SerializeField] Vector2 maxBounds = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

    Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        Vector3 smoothPos = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, Mathf.Max(0.0001f, smoothTime));

        if (useBounds)
        {
            smoothPos.x = Mathf.Clamp(smoothPos.x, minBounds.x, maxBounds.x);
            smoothPos.y = Mathf.Clamp(smoothPos.y, minBounds.y, maxBounds.y);
        }

        transform.position = smoothPos;
    }

    public void SetTarget(Transform t) => target = t;
}
