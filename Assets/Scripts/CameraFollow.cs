using UnityEngine;
using UnityEngine.InputSystem;

public class RobotFirstPersonCamera : MonoBehaviour
{
    [Header("Target (Robot)")]
    [SerializeField] private Transform robotBody; // сам робот (или его голова)

    [Header("Camera Rotation Limits")]
    [SerializeField] private float maxHorizontalAngle = 60f;   // влево-вправо от forward
    [SerializeField] private float maxVerticalAngle = 45f;     // вверх-вниз

    [Header("Sensitivity")]
    [SerializeField] private float gamepadSensitivity = 2f;
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Position")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.5f, 0.2f); // смещение внутри робота

    private RobotControls controls;
    private Vector2 lookInput;
    private float yawOffset = 0f;
    private float pitchOffset = 0f;

    void Awake()
    {
        controls = new RobotControls();
        controls.Camera.Enable();
        controls.Camera.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Camera.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    void Start()
    {
        if (robotBody == null)
            robotBody = transform.parent; // предполагаем, что камера внутри робота
        transform.localPosition = localOffset;
    }

    void LateUpdate()
    {
        if (robotBody == null) return;

        Vector2 input = Vector2.zero;
        if (Gamepad.current != null)
        {
            input = lookInput * gamepadSensitivity * Time.deltaTime;
        }
        else
        {
            input = Mouse.current.delta.ReadValue() * mouseSensitivity * 0.01f;
        }

        yawOffset += input.x;
        pitchOffset -= input.y;
        yawOffset = Mathf.Clamp(yawOffset, -maxHorizontalAngle, maxHorizontalAngle);
        pitchOffset = Mathf.Clamp(pitchOffset, -maxVerticalAngle, maxVerticalAngle);

        // Вращение камеры относительно робота
        Quaternion robotRotation = robotBody.rotation;
        Quaternion cameraRotation = robotRotation * Quaternion.Euler(pitchOffset, yawOffset, 0f);
        transform.rotation = cameraRotation;

        // Камера следует за роботом, но сохраняет смещение в локальных координатах
        transform.position = robotBody.position + robotBody.TransformDirection(localOffset);
    }

    public void ResetView()
    {
        yawOffset = 0f;
        pitchOffset = 0f;
    }

    void OnDestroy() => controls?.Dispose();
}