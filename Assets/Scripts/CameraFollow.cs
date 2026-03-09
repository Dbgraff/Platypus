using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target & Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -3f);

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2.5f;

    [Header("Gamepad Look")]
    [SerializeField] private float gamepadSensitivity = 1.5f;
    [SerializeField] private float deadzone = 0.15f;

    [Header("Pitch Limits")]
    [SerializeField] private float pitchMin = -80f;
    [SerializeField] private float pitchMax = 80f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 12f;

    [Header("Smooth")]
    [SerializeField] private float smoothSpeed = 0.12f;

    [Header("Device-specific")]
    [SerializeField] private float initialDistanceMouse = 5f;
    [SerializeField] private float initialDistanceGamepad = 8f;

    // Private
    private RobotControls controls;
    private float currentDistance;
    private float yaw;
    private float pitch;
    private bool cursorLocked = true;

    void Awake()
    {
        controls = new RobotControls();
        controls.Camera.Enable();
    }

    void Start()
    {
        bool hasGamepad = Gamepad.current != null;

        currentDistance = hasGamepad ? initialDistanceGamepad : initialDistanceMouse;

        if (hasGamepad)
        {
            offset = new Vector3(0f, 1.5f, -8f);
        }

        UpdateCursorLock();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector2 lookInput = Vector2.zero;

        // 1. PRIORITY: Gamepad Right Stick (если подключен)
        if (Gamepad.current != null)
        {
            Vector2 stick = controls.Camera.Look.ReadValue<Vector2>();
            if (stick.magnitude > deadzone)
            {
                lookInput = (stick.normalized * (stick.magnitude - deadzone) / (1f - deadzone)) * gamepadSensitivity;
            }
        }
        // 2. Fallback: Mouse (если нет геймпада или stick idle)
        else
        {
            lookInput = Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime;
        }

        yaw += lookInput.x;
        pitch -= lookInput.y;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // 3. Zoom (Mouse Wheel only — геймпад без, ок для sim)
        float scroll = Mouse.current.scroll.y.ReadValue() * zoomSpeed * Time.deltaTime;
        currentDistance -= scroll;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // 4. Base Yaw от робота (forward follow!)
        float targetYaw = target.eulerAngles.y;

        // 5. Rotation: robot yaw + relative yaw/pitch
        Quaternion rotation = Quaternion.Euler(pitch, targetYaw + yaw, 0f);

        // 6. Position: сферическая орбита
        Vector3 direction = rotation * Vector3.forward;
        Vector3 desiredPosition = target.position - direction * currentDistance + new Vector3(0, offset.y, 0);

        // 7. Smooth
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, smoothSpeed);
    }

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            yaw = 0f;
            pitch = 0f;
        }

        if ((Gamepad.current != null && Gamepad.current.leftStickButton.wasPressedThisFrame) ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            cursorLocked = !cursorLocked;
            UpdateCursorLock();
        }
    }

    private void UpdateCursorLock()
    {
        Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !cursorLocked;
    }

    public void ResetCamera()
    {
        yaw = 0f;
        pitch = 0f;
    }

    void OnDestroy()
    {
        controls?.Disable();
    }
}