using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float slopeForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.6f;

    private Rigidbody _rb;
    private Vector2 _moveInput;
    private Vector3 _moveDirection;
    private Vector3 _camForward;
    private Vector3 _camRight;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();

        if (_rb == null)
        {
            Debug.LogError("Rigidbody not found on the GameObject!");
            return;
        }

        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;

        // Initialize camera-relative directions once (ignore Y)
        Camera cam = Camera.main;
        if (cam != null)
        {
            _camForward = cam.transform.forward;
            _camForward.y = 0f;
            _camForward.Normalize();

            _camRight = cam.transform.right;
            _camRight.y = 0f;
            _camRight.Normalize();
        }
        else
        {
            _camForward = Vector3.forward;
            _camRight = Vector3.right;
        }
    }

    void Update()
    {
        // Reading input from the new Input System
        // This reads WASD or Arrow keys automatically
        if (Keyboard.current != null)
        {
            float h = 0;
            float v = 0;

            if (Keyboard.current.wKey.isPressed) v += 1;
            if (Keyboard.current.sKey.isPressed) v -= 1;
            if (Keyboard.current.aKey.isPressed) h -= 1;
            if (Keyboard.current.dKey.isPressed) h += 1;

            _moveInput = new Vector2(h, v).normalized;
        }
    }

    void FixedUpdate()
    {
        if (_rb == null) return;

        ApplyMovement();
    }

    private void ApplyMovement()
    {
        // Convert Vector2 input to world space using camera-relative directions (ignore Y)
        Vector3 worldInput = _camRight * _moveInput.x + _camForward * _moveInput.y;

        if (worldInput.magnitude < 0.1f)
        {
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            return;
        }

        _moveDirection = worldInput;

        if (OnSlope(out Vector3 slopeNormal))
        {
            _moveDirection = Vector3.ProjectOnPlane(worldInput, slopeNormal).normalized;
            _rb.AddForce(-slopeNormal * slopeForce, ForceMode.Force);
        }

        Vector3 targetVelocity = _moveDirection * moveSpeed;
        targetVelocity.y = _rb.linearVelocity.y; 
        
        _rb.linearVelocity = targetVelocity;
    }

    private bool OnSlope(out Vector3 hitNormal)
    {
        hitNormal = Vector3.up;
        if (Physics.Raycast(_rb.transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            hitNormal = hit.normal;
            return Mathf.Abs(hitNormal.y) < 0.99f; // More reliable slope detection
        }
        return false;
    }
}