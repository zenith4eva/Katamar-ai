using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Custom attribute to make fields read-only in Inspector
public class ReadOnlyAttribute : PropertyAttribute
{
    public ReadOnlyAttribute() { }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float maxSpeed = 15f;
    public float acceleration = 20f;
    public float deceleration = 10f;
    
    [Header("Physics")]
    public float jumpHeight = 2f; // Desired jump height in world units
    public LayerMask groundLayer = 1;
    
    [Header("Pickup System")]
    public float baseSize = 1f;
    public Transform pickupParent;
    public float totalPoints = 0f;
    
    
    [Header("Current Status (Read-Only)")]
    [SerializeField, ReadOnly] private float currentSize;
    
    [Header("Size Growth")]
    public float sizeGrowthRate = 0.1f; // How much size increases per point
    public float maxSize = 5f; // Maximum player size
    public float massGrowthRate = 0.5f; // How much mass increases per point
    
    [Header("Growth Animation")]
    public float growthDuration = 0.5f; // How long the growth animation takes
    public AnimationCurve growthEase = AnimationCurve.EaseInOut(0, 0, 1, 1); // Easing curve for growth
    public bool useJuicyGrowth = true; // Enable/disable smooth growth animation
    
    [Header("Juice Effects")]
    public bool enableScreenShake = true; // Enable screen shake on growth
    public float shakeIntensity = 0.1f; // How intense the screen shake is
    public float shakeDuration = 0.2f; // How long the screen shake lasts
    
    [Header("Rolling Physics")]
    public float spinSpeedMultiplier = 1f;
    
    private Rigidbody rb;
    private Transform sphereTransform;
    private Camera playerCamera;
    private bool isGrounded;
    private List<MonoBehaviour> pickedUpObjects = new List<MonoBehaviour>();
    
    // Camera direction smoothing
    private Vector3 lastValidCameraForward = Vector3.forward;
    private Vector3 lastValidCameraRight = Vector3.right;
    
    // Growth animation variables
    [SerializeField, ReadOnly] private float currentDisplaySize = 1f; // The currently displayed size (for animation)
    private float targetSize = 1f; // The target size we're animating towards
    private Coroutine growthCoroutine;
    
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
        // Find the sphere child object
        sphereTransform = transform.Find("Sphere");
        if (sphereTransform == null)
        {
            Debug.LogError("PlayerController requires a 'Sphere' child object!");
            return;
        }
        
        // Get or add Rigidbody to the Player object (top level)
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Remove Rigidbody from sphere child if it exists
        Rigidbody sphereRb = sphereTransform.GetComponent<Rigidbody>();
        if (sphereRb != null)
        {
            DestroyImmediate(sphereRb);
        }
        
        playerCamera = Camera.main;
        
        // Configure rigidbody for rolling
        rb.freezeRotation = false;
        rb.centerOfMass = Vector3.zero;
        
        // Setup pickup parent if not assigned
        if (pickupParent == null)
        {
            pickupParent = transform.Find("Pickup Parent");
            if (pickupParent == null)
            {
                pickupParent = transform;
            }
        }
    }
    
    void Update()
    {
        CheckGrounded();
        HandleJump();
        
        // Update current size for Inspector display
        currentSize = GetCurrentSize();
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
        if (playerCamera == null)
        {
            // Fallback to world directions if camera is null
            Vector3 fallbackMoveDirection = new Vector3(horizontal, 0, vertical).normalized;
            Vector3 fallbackDesiredVelocity = fallbackMoveDirection * moveSpeed;
            Vector3 fallbackVelocityChange = fallbackDesiredVelocity - new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            fallbackVelocityChange = Vector3.ClampMagnitude(fallbackVelocityChange, acceleration * Time.fixedDeltaTime);
            rb.AddForce(fallbackVelocityChange, ForceMode.VelocityChange);
            return;
        }
        
        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0;
        
        Vector3 cameraRight = playerCamera.transform.right;
        cameraRight.y = 0;
        
        // Check if camera vectors are valid (not too small)
        if (cameraForward.magnitude < 0.1f)
        {
            // If camera is looking too vertically, use last valid direction
            cameraForward = lastValidCameraForward;
        }
        else
        {
            cameraForward.Normalize();
            // Smooth the camera direction to prevent stuttering
            lastValidCameraForward = Vector3.Slerp(lastValidCameraForward, cameraForward, Time.fixedDeltaTime * 10f);
            cameraForward = lastValidCameraForward;
        }
        
        if (cameraRight.magnitude < 0.1f)
        {
            // If camera is looking too vertically, use last valid direction
            cameraRight = lastValidCameraRight;
        }
        else
        {
            cameraRight.Normalize();
            // Smooth the camera direction to prevent stuttering
            lastValidCameraRight = Vector3.Slerp(lastValidCameraRight, cameraRight, Time.fixedDeltaTime * 10f);
            cameraRight = lastValidCameraRight;
        }
        
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
            float currentRadius = 0.5f; // Base sphere radius (collider scales with transform)
            float rotationSpeed = horizontalVelocity.magnitude * 360f / (2f * Mathf.PI * currentRadius) * spinSpeedMultiplier;
            rb.AddTorque(rotationAxis * rotationSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }
    
    void HandleJump()
    {
        if (jumpInput && isGrounded)
        {
            // Calculate required velocity to reach desired jump height
            // Using physics: v = sqrt(2 * g * h) where g is gravity and h is height
            float requiredVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
            
            // Set vertical velocity directly to achieve consistent jump height
            Vector3 currentVelocity = rb.linearVelocity;
            currentVelocity.y = requiredVelocity;
            rb.linearVelocity = currentVelocity;
            
            jumpInput = false; // Reset jump input
        }
    }
    
    void CheckGrounded()
    {
        // Ground check that accounts for current player size
        // Use sphere radius + small buffer for ground detection
        float sphereRadius = 0.5f * currentDisplaySize; // Base radius (0.5) * current scale
        float groundCheckDistance = sphereRadius + 0.1f; // Add small buffer
        
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }
    
    // Pickup System Methods
    public float GetCurrentSize()
    {
        return baseSize + (totalPoints * sizeGrowthRate);
    }
    
    public float GetCurrentDisplaySize()
    {
        return currentDisplaySize;
    }
    
    public bool CanPickup(float objectSize)
    {
        return GetCurrentSize() >= objectSize;
    }
    
    public void OnPickupSuccess(MonoBehaviour pickupObject)
    {
        pickedUpObjects.Add(pickupObject);
        
        // Get point value from pickup object
        var getPointValueMethod = pickupObject.GetType().GetMethod("GetPointValue");
        float points = 0f;
        if (getPointValueMethod != null)
        {
            points = (float)getPointValueMethod.Invoke(pickupObject, null);
        }
        
        // Get screen shake intensity from pickup object
        var getScreenShakeMethod = pickupObject.GetType().GetMethod("GetScreenShakeIntensity");
        float pickupShakeIntensity = shakeIntensity; // Default to player's shake intensity
        if (getScreenShakeMethod != null)
        {
            pickupShakeIntensity = (float)getScreenShakeMethod.Invoke(pickupObject, null);
        }
        
        AddPoints(points, pickupShakeIntensity);
    }
    
    public void AddPoints(float points, float customShakeIntensity = -1f)
    {
        totalPoints += points;
        UpdatePlayerSize(customShakeIntensity);
    }
    
    void UpdatePlayerSize(float customShakeIntensity = -1f)
    {
        float newSize = GetCurrentSize();
        
        // Clamp size to maximum
        newSize = Mathf.Min(newSize, maxSize);
        
        // Check if size actually increased (for screen shake)
        bool sizeIncreased = newSize > targetSize;
        targetSize = newSize;
        
        // Update rigidbody mass for heavier feel (immediate)
        if (rb != null)
        {
            rb.mass = 1f + (totalPoints * massGrowthRate);
        }
        
        // Start smooth growth animation
        if (useJuicyGrowth)
        {
            if (growthCoroutine != null)
            {
                StopCoroutine(growthCoroutine);
            }
            growthCoroutine = StartCoroutine(AnimateGrowth());
        }
        else
        {
            // Instant growth (no animation)
            currentDisplaySize = targetSize;
            UpdateSphereScale();
        }
        
        // Add screen shake for juice - only when size actually increases
        if (sizeIncreased && enableScreenShake && playerCamera != null)
        {
            // Use custom shake intensity if provided, otherwise use default
            float shakeIntensityToUse = customShakeIntensity > 0f ? customShakeIntensity : shakeIntensity;
            StartCoroutine(ScreenShake(shakeIntensityToUse));
        }
        
        Debug.Log($"Player size updated: {newSize}, Points: {totalPoints}, Mass: {rb.mass}");
    }
    
    IEnumerator AnimateGrowth()
    {
        float startSize = currentDisplaySize;
        float elapsed = 0f;
        
        while (elapsed < growthDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / growthDuration;
            
            // Apply easing curve
            float easedProgress = growthEase.Evaluate(progress);
            
            // Interpolate between start and target size
            currentDisplaySize = Mathf.Lerp(startSize, targetSize, easedProgress);
            
            // Update visual scale
            UpdateSphereScale();
            
            yield return null;
        }
        
        // Ensure we end exactly at target size
        currentDisplaySize = targetSize;
        UpdateSphereScale();
        
        growthCoroutine = null;
    }
    
    void UpdateSphereScale()
    {
        // Update sphere child scale (only the visual/collision sphere grows)
        if (sphereTransform != null)
        {
            sphereTransform.localScale = Vector3.one * currentDisplaySize;
        }
    }
    
    IEnumerator ScreenShake(float customIntensity = -1f)
    {
        Vector3 originalPosition = playerCamera.transform.localPosition;
        float elapsed = 0f;
        
        // Use custom intensity if provided, otherwise use default
        float intensityToUse = customIntensity > 0f ? customIntensity : shakeIntensity;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / shakeDuration;
            
            // Decrease shake intensity over time
            float currentIntensity = intensityToUse * (1f - progress);
            
            // Random shake offset
            Vector3 shakeOffset = new Vector3(
                Random.Range(-currentIntensity, currentIntensity),
                Random.Range(-currentIntensity, currentIntensity),
                0f
            );
            
            playerCamera.transform.localPosition = originalPosition + shakeOffset;
            
            yield return null;
        }
        
        // Return camera to original position
        playerCamera.transform.localPosition = originalPosition;
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
        Gizmos.DrawWireSphere(transform.position, 0.5f); // Base radius (visual scale handled by sphere transform)
    }
}