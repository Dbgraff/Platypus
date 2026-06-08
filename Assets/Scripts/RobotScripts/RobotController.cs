using UnityEngine;
using UnityEngine.InputSystem;

public class RobotController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider[] leftWheels;
    [SerializeField] private WheelCollider[] rightWheels;

    [Header("Power")]
    [SerializeField] private float maxMotorForce = 200f;
    [SerializeField] private float maxForwardSpeed = 6f;
    [SerializeField] private float maxReverseSpeed = 3f;

    [Header("Keyboard Mode")]
    [SerializeField] private bool startInSplitMode = true;

    private RobotControls controls;
    private Rigidbody rb;

    private float leftTrackInput = 0f;
    private float rightTrackInput = 0f;
    private float throttleInput = 0f; 
    private float steerInput = 0f;

    private bool isSplitMode = true;

    private bool isGamepadConnected => Gamepad.current != null;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new RobotControls();
        controls.Robot.Enable();

        controls.Robot.LeftTrack.performed += ctx => leftTrackInput = ctx.ReadValue<float>();
        controls.Robot.LeftTrack.canceled += ctx => leftTrackInput = 0f;

        controls.Robot.RightTrack.performed += ctx => rightTrackInput = ctx.ReadValue<float>();
        controls.Robot.RightTrack.canceled += ctx => rightTrackInput = 0f;

        controls.Robot.Throttle.performed += ctx => throttleInput = ctx.ReadValue<float>();
        controls.Robot.Throttle.canceled += ctx => throttleInput = 0f;

        controls.Robot.Steer.performed += ctx => steerInput = ctx.ReadValue<float>();
        controls.Robot.Steer.canceled += ctx => steerInput = 0f;

        controls.Robot.ToggleControlMode.performed += ctx => ToggleMode();

        isSplitMode = startInSplitMode;
    }

    private void ToggleMode()
    {
        if (!isGamepadConnected)
        {
            isSplitMode = !isSplitMode;
            Debug.Log($"Keyboard mode: {(isSplitMode ? "SPLIT (W/S + O/L)" : "CLASSIC WASD (W/S = forward/back, A/D = turn)")}");
        }
    }

    void FixedUpdate()
    {

        float left = 0f, right = 0f;

        if (isGamepadConnected)
        {
            left = leftTrackInput;
            right = rightTrackInput;
        }
        else
        {
            if (isSplitMode)
            {
                left = leftTrackInput;
                right = rightTrackInput;
            }
            else
            {
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

        float leftTorque = CalculateTorque(leftInput, speed);
        float rightTorque = CalculateTorque(rightInput, speed);

        foreach (var w in leftWheels) if (w) w.motorTorque = leftTorque;
        foreach (var w in rightWheels) if (w) w.motorTorque = rightTorque;
    }

    private float CalculateTorque(float input, float currentSpeed)
    {
        if (Mathf.Abs(input) < 0.05f) return 0f;

        float targetDirection = Mathf.Sign(input);
        float maxAllowedSpeed = (targetDirection > 0) ? maxForwardSpeed : maxReverseSpeed;

        float speedInDirection = Vector3.Dot(rb.linearVelocity, transform.forward) * targetDirection;
        if (speedInDirection >= maxAllowedSpeed) return 0f;

        float speedFactor = Mathf.Clamp01(1f - speedInDirection / maxAllowedSpeed);
        return input * maxMotorForce * speedFactor;
    }

    private void OnDestroy() => controls?.Dispose();

    /// <summary>
    /// Принудительная подача команд на гусеницы (используется системой автовозврата).
    /// Значения left и right в диапазоне [-1, 1].
    /// </summary>
    public void ApplyManualControl(float leftNormalized, float rightNormalized)
    {
        ApplyTrackForces(leftNormalized, rightNormalized);
    }

    public float GetMaxForwardSpeed()
    {
        return maxForwardSpeed;
    }
}