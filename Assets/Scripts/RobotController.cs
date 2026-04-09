using UnityEngine;
using UnityEngine.InputSystem;

public class RobotController_Tank : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider[] leftWheels;
    [SerializeField] private WheelCollider[] rightWheels;

    [Header("Power")]
    [SerializeField] private float maxMotorForce = 200f;
    [SerializeField] private float maxBrakeForce = 300f;
    [SerializeField] private float maxForwardSpeed = 6f;
    [SerializeField] private float maxReverseSpeed = 3f;

    [Header("Keyboard Mode")]
    [SerializeField] private bool useSplitKeyboardControls = true;  // true = W/S + O/L, false = WASD классика
    [SerializeField] private KeyCode toggleModeKey = KeyCode.M;

    private RobotControls controls;
    private Rigidbody rb;

    private float leftTrackInput = 0f;
    private float rightTrackInput = 0f;

    // Для классического режима
    private float throttleInput = 0f;
    private float steerInput = 0f;

    private bool isGamepadConnected => Gamepad.current != null;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new RobotControls();
        controls.Robot.Enable();

        // Подписка на события (для геймпада и раздельной клавиатуры)
        controls.Robot.LeftTrack.performed += ctx => leftTrackInput = ctx.ReadValue<float>();
        controls.Robot.LeftTrack.canceled += ctx => leftTrackInput = 0f;

        controls.Robot.RightTrack.performed += ctx => rightTrackInput = ctx.ReadValue<float>();
        controls.Robot.RightTrack.canceled += ctx => rightTrackInput = 0f;

        // Для классического режима
        controls.Robot.Throttle.performed += ctx => throttleInput = ctx.ReadValue<float>();
        controls.Robot.Throttle.canceled += ctx => throttleInput = 0f;
        controls.Robot.Steer.performed += ctx => steerInput = ctx.ReadValue<float>();
        controls.Robot.Steer.canceled += ctx => steerInput = 0f;
    }

    void Update()
    {
        //// Переключение режима клавиатуры (только если нет геймпада)
        //if (!isGamepadConnected && Input.GetKeyDown(toggleModeKey))
        //{
        //    useSplitKeyboardControls = !useSplitKeyboardControls;
        //    Debug.Log($"Keyboard control mode: {(useSplitKeyboardControls ? "SPLIT (W/S + O/L)" : "CLASSIC (WASD)")}");
        //}
    }

    void FixedUpdate()
    {
        float left = 0f, right = 0f;
        bool isReversing = false;

        if (isGamepadConnected)
        {
            // Геймпад всегда в раздельном режиме
            left = leftTrackInput;
            right = rightTrackInput;
        }
        else
        {
            if (useSplitKeyboardControls)
            {
                // Раздельное управление клавой (W/S левая, O/L правая)
                left = leftTrackInput;
                right = rightTrackInput;
            }
            else
            {
                // Классическое WASD: пересчитываем в раздельные сигналы
                float forward = throttleInput;
                float turn = steerInput;

                left = Mathf.Clamp(forward + turn, -1f, 1f);
                right = Mathf.Clamp(forward - turn, -1f, 1f);
            }
        }

        ApplyTrackForces(left, right);
    }

    private void ApplyTrackForces(float leftInput, float rightInput)
    {
        float speed = rb.linearVelocity.magnitude;
        bool movingForward = Vector3.Dot(rb.linearVelocity, transform.forward) > 0;

        // Ограничение скорости
        float leftTorque = 0f, rightTorque = 0f;
        float leftBrake = 0f, rightBrake = 0f;

        // Левая гусеница
        if (Mathf.Abs(leftInput) > 0.05f)
        {
            float targetDir = Mathf.Sign(leftInput);
            float speedFactor = (targetDir > 0) ? Mathf.Clamp01(1f - speed / maxForwardSpeed) : Mathf.Clamp01(1f - speed / maxReverseSpeed);
            leftTorque = leftInput * maxMotorForce * speedFactor;
        }
        else
        {
            // имитация сопротивления
            leftBrake = maxBrakeForce * 0.2f;
        }

        // Правая гусеница
        if (Mathf.Abs(rightInput) > 0.05f)
        {
            float targetDir = Mathf.Sign(rightInput);
            float speedFactor = (targetDir > 0) ? Mathf.Clamp01(1f - speed / maxForwardSpeed) : Mathf.Clamp01(1f - speed / maxReverseSpeed);
            rightTorque = rightInput * maxMotorForce * speedFactor;
        }
        else
        {
            rightBrake = maxBrakeForce * 0.2f;
        }

        // Применяем к колёсам
        foreach (var w in leftWheels) { if (w) { w.motorTorque = leftTorque; w.brakeTorque = leftBrake; } }
        foreach (var w in rightWheels) { if (w) { w.motorTorque = rightTorque; w.brakeTorque = rightBrake; } }
    }

    private void OnDestroy() => controls?.Dispose();
}