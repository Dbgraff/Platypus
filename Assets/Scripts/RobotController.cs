using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RobotController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("Components")]
    [SerializeField] private WheelCollider[] leftWheels;
    [SerializeField] private WheelCollider[] rightWheels;

    [Header("Settings")]
    [SerializeField] private float maxMotorForce = 100f;
    [SerializeField] private float maxBrakeForce = 200f;
    [SerializeField] private float maxSpeed = 5f;

    [Header("Trigger sensetivity")]
    [SerializeField][Range(0.1f, 1f)] private float throttleSensitivity = 0.9f;  // газ
    [SerializeField][Range(0.1f, 1f)] private float brakeSensitivity = 1.0f;     // Тормоз 

    private Rigidbody rb;
    private RobotControls controls;

    private float throttleInput;
    private float brakeInput;
    private float steerInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new RobotControls();


        controls.Robot.Throttle.performed += ctx => throttleInput = ctx.ReadValue<float>();
        controls.Robot.Throttle.canceled += ctx => throttleInput = 0f;

        controls.Robot.Brake.performed += ctx => brakeInput = ctx.ReadValue<float>();
        controls.Robot.Brake.canceled += ctx => brakeInput = 0f;

        controls.Robot.Steer.performed += ctx => steerInput = ctx.ReadValue<float>();
        controls.Robot.Steer.canceled += ctx => steerInput = 0f;

        controls.Robot.Enable();
    }

    private void OnEnable()
    {
        if (controls != null) { controls.Robot.Enable(); }
    }

    private void OnDisable()
    {
        if (controls != null) { controls.Robot.Disable(); }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        float speed = rb.linearVelocity.magnitude;

        // Ограничиваем газ по скорости (плавно, не резко)
        float speedFactor = Mathf.Clamp01(1f - (speed / maxSpeed));
        float limitedThrottle = throttleInput * speedFactor;

        // Базовый торк
        float baseTorque = limitedThrottle * maxMotorForce * throttleSensitivity;

        // Дифференциал для поворота (ТАНК-STYLE! Правильно!)
        // steerInput >0 = поворот направо: ЛЕВЫЙ трек быстрее, ПРАВЫЙ медленнее
        float diffFactor = 0.6f; // Меньше 0.8 для плавности
        float leftTorque = baseTorque + (steerInput * maxMotorForce * diffFactor);
        float rightTorque = baseTorque - (steerInput * maxMotorForce * diffFactor);

        // Тормоз (применяем ко всем)
        float appliedBrake = brakeInput * maxBrakeForce * brakeSensitivity;

        // Применяем к левым колесам
        foreach (var wheel in leftWheels)
        {
            if (wheel != null)
            {
                wheel.motorTorque = leftTorque;
                wheel.brakeTorque = appliedBrake;
            }
        }

        // Применяем к правым
        foreach (var wheel in rightWheels)
        {
            if (wheel != null)
            {
                wheel.motorTorque = rightTorque;
                wheel.brakeTorque = appliedBrake;
            }
        }
    }
}
