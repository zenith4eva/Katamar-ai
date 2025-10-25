using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float maxSpeed = 15f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    
    [Header("Physics")]
    public float jumpForce = 10f;
    public LayerMask groundLayer = 1;
    
    [Header("Pickup System")]
    public float size = 1f;
    public Transform pickupParent;
    
    [Header("Rolling Physics")]
    public float spinSpeedMultiplier = 1f;
    
    private Rigidbody rb;
    private Camera playerCamera;
    private bool isGrounded;
    private List<MonoBehaviour> pickedUpObjects = new List<MonoBehaviour>();
    
    // Input System
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private bool jumpInput;
    
    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Jump.performed += OnJumpPerformed;
    }
    
    void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCanceled;
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        
        inputActions?.Disable();
    }
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera = Camera.main;
        
        // Configure rigidbody for rolling
        rb.freezeRotation = false;
        rb.centerOfMass = Vector3.zero;
        
        // Setup pickup parent if not assigned
        if (pickupParent == null)
        {
            pickupParent = transform;
        }
    }
    
    void Update()
    {
        CheckGrounded();
        HandleJump();
    }
    
    void FixedUpdate()
    {
        HandleMovement();
    }
    
    void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }
    
    void OnJumpPerformed(InputAction.CallbackContext context)
    {
        jumpInput = true;
    }
    
    void HandleMovement()
    {
        // Get input from Input System
        float horizontal = moveInput.x; // A/D
        float vertical = moveInput.y;   // W/S
        
        if (horizontal == 0 && vertical == 0)
        {
            // Apply deceleration when no input
            rb.linearVelocity = new Vector3(
                Mathf.Lerp(rb.linearVelocity.x, 0, deceleration * Time.fixedDeltaTime),
                rb.linearVelocity.y,
                Mathf.Lerp(rb.linearVelocity.z, 0, deceleration * Time.fixedDeltaTime)
            );
            return;
        }
        
        // Get camera direction (projected on ground plane)
        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        
        Vector3 cameraRight = playerCamera.transform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();
        
        // Calculate movement direction relative to camera
        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
        
        // Calculate desired velocity
        Vector3 desiredVelocity = moveDirection * moveSpeed;
        
        // Apply force to reach desired velocity
        Vector3 velocityChange = desiredVelocity - new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        velocityChange = Vector3.ClampMagnitude(velocityChange, acceleration * Time.fixedDeltaTime);
        
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
        
        // Limit max speed
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
        
        // Add rolling effect by rotating the ball
        if (horizontalVelocity.magnitude > 0.1f)
        {
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, horizontalVelocity.normalized);
            float rotationSpeed = horizontalVelocity.magnitude * 360f / (2f * Mathf.PI * 0.5f) * spinSpeedMultiplier; // 0.5f is sphere radius
            rb.AddTorque(rotationAxis * rotationSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }
    
    void HandleJump()
    {
        if (jumpInput && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpInput = false; // Reset jump input
        }
    }
    
    void CheckGrounded()
    {
        // Simple ground check using raycast
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.6f, groundLayer);
    }
    
    // Pickup System Methods
    public bool CanPickup(float objectSize)
    {
        return size >= objectSize;
    }
    
    public void OnPickupSuccess(MonoBehaviour pickupObject)
    {
        pickedUpObjects.Add(pickupObject);
        
        // You can add logic here to increase player size, score, etc.
        // For example: size += pickupObject.size * 0.1f;
    }
    
    public void DropAllPickups()
    {
        foreach (MonoBehaviour pickup in pickedUpObjects)
        {
            var resetMethod = pickup.GetType().GetMethod("ResetPickup");
            if (resetMethod != null)
            {
                resetMethod.Invoke(pickup, null);
            }
        }
        pickedUpObjects.Clear();
    }
    
    public int GetPickupCount()
    {
        return pickedUpObjects.Count;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw ground check ray
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * 0.6f);
        
        // Draw size indicator
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, size * 0.5f);
    }
}