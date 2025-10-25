using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;
    public float mouseSensitivity = 2f;
    public float distance = 5f;
    public float height = 2f;
    public float smoothSpeed = 10f;
    
    [Header("Camera Limits")]
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    
    private float currentX = 0f;
    private float currentY = 0f;
    private Vector3 currentVelocity;
    
    // Input System
    private InputSystem_Actions inputActions;
    private Vector2 mouseInput;
    private bool escapePressed;
    
    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        
        inputActions.Player.Look.performed += OnLookPerformed;
        inputActions.Player.Look.canceled += OnLookCanceled;
        inputActions.Player.Escape.performed += OnEscapePerformed;
    }
    
    void OnDisable()
    {
        inputActions.Player.Look.performed -= OnLookPerformed;
        inputActions.Player.Look.canceled -= OnLookCanceled;
        inputActions.Player.Escape.performed -= OnEscapePerformed;
        
        inputActions?.Disable();
    }
    
    void Start()
    {
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Get mouse input from Input System
        float mouseX = mouseInput.x * mouseSensitivity;
        float mouseY = mouseInput.y * mouseSensitivity;
        
        currentX += mouseX;
        currentY -= mouseY;
        
        // Clamp vertical rotation
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
        
        // Calculate desired position
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 direction = rotation * Vector3.back;
        Vector3 desiredPosition = target.position + direction * distance + Vector3.up * height;
        
        // Smoothly move camera to desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);
        
        // Look at target
        transform.LookAt(target.position + Vector3.up * height);
    }
    
    void Update()
    {
        // Toggle cursor lock with Escape
        if (escapePressed)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            escapePressed = false; // Reset escape input
        }
    }
    
    void OnLookPerformed(InputAction.CallbackContext context)
    {
        mouseInput = context.ReadValue<Vector2>();
    }
    
    void OnLookCanceled(InputAction.CallbackContext context)
    {
        mouseInput = Vector2.zero;
    }
    
    void OnEscapePerformed(InputAction.CallbackContext context)
    {
        escapePressed = true;
    }
}
