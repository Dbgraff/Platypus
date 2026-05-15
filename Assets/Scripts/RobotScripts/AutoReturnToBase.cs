using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class AutoReturnToBase : MonoBehaviour
{
    [Header("База")]
    [SerializeField] private Transform basePoint;
    [SerializeField] private float signalRange = 100f;

    [Header("Параметры движения")]
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float arrivalDistance = 3f;

    [Header("Регуляторы")]
    [SerializeField] private float steeringGain = 2.5f;
    [SerializeField] private float speedGain = 1.2f;

    [Header("Диагностика застревания")]
    [SerializeField] private float stuckTimeThreshold = 2f;
    [SerializeField] private float stuckSpeedThreshold = 0.2f;

    [Header("Управление")]
    [SerializeField] private InputActionReference returnAction;
    [SerializeField] private InputActionReference cancelAction;

    private RobotController controller;
    private Rigidbody rb;
    private bool isReturning;
    private bool signalLost;

    private float stuckTimer = 0f;
    private bool isUnstucking = false;
    private float unstuckTimer = 0f;
    private float unstuckDirection = 1f;

    // Отладка
    private float debugLogTimer = 0f;
    private const float debugLogInterval = 0.5f;

    public bool IsReturning => isReturning;
    public bool SignalLost => signalLost;
    public float DistanceToBase => basePoint ? Vector3.Distance(transform.position, basePoint.position) : 0f;
    public float SignalStrength => basePoint ? Mathf.Clamp01(1f - DistanceToBase / signalRange) : 0f;

    public event Action OnReturnStarted;
    public event Action OnReturnCancelled;
    public event Action OnBaseReached;
    public event Action OnSignalLostEvent;
    public event Action OnSignalRestored;

    private void Awake()
    {
        controller = GetComponent<RobotController>();
        rb = GetComponent<Rigidbody>();

        if (returnAction != null)
        {
            returnAction.action.Enable();
            returnAction.action.performed += OnReturnPerformed;
        }
        else
            Debug.LogError("AutonomousReturn: не назначен returnAction!");

        if (cancelAction != null)
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += OnCancelPerformed;
        }
        else
            Debug.LogError("AutonomousReturn: не назначен cancelAction!");

        Debug.Log($"[AutonomousReturn] Инициализация. Controller: {(controller != null ? "найден" : "ОТСУТСТВУЕТ")}, Rigidbody: {(rb != null ? "найден" : "ОТСУТСТВУЕТ")}");
    }

    private void OnDestroy()
    {
        if (returnAction != null)
        {
            returnAction.action.performed -= OnReturnPerformed;
            returnAction.action.Disable();
        }
        if (cancelAction != null)
        {
            cancelAction.action.performed -= OnCancelPerformed;
            cancelAction.action.Disable();
        }
    }

    private void Start()
    {
        if (basePoint == null)
        {
            Debug.LogError("AutonomousReturn: не назначена базовая точка!");
            enabled = false;
        }
    }

    private void Update()
    {
        if (basePoint == null) return;

        if (!isReturning && DistanceToBase > signalRange)
        {
            Debug.Log($"[AutonomousReturn] Потеря связи! Расстояние: {DistanceToBase:F1}м > {signalRange}м");
            StartReturn(automatic: true);
        }
    }

    private void FixedUpdate()
    {
        if (!isReturning) return;

        if (controller == null)
        {
            Debug.LogError("[AutonomousReturn] RobotController отсутствует! Остановка возврата.");
            CancelReturn();
            return;
        }

        RunAutonomousLogic();
    }

    private void RunAutonomousLogic()
    {
        Vector3 toBase = basePoint.position - transform.position;
        toBase.y = 0f;
        float distance = toBase.magnitude;

        // Периодический лог
        debugLogTimer += Time.fixedDeltaTime;
        if (debugLogTimer >= debugLogInterval)
        {
            debugLogTimer = 0f;
            Debug.Log($"[AutonomousReturn] Дист: {distance:F1}м, Скорость: {rb.linearVelocity.magnitude:F2}м/с, Позиция: {transform.position}");
        }

        // Прибытие
        if (distance <= arrivalDistance)
        {
            Debug.Log($"[AutonomousReturn] ✓ Прибыл на базу! Дистанция: {distance:F1}м");
            ArriveAtBase();
            return;
        }

        // Угол к цели
        float targetAngle = Mathf.Atan2(toBase.x, toBase.z) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.y;
        float angleError = Mathf.DeltaAngle(currentAngle, targetAngle);

        // Застревание
        if (!isUnstucking)
            CheckStuck();
        else
        {
            UnstuckRoutine();
            return;
        }

        // Расчёт команд
        CalculateTrackInputs(angleError, distance, out float leftInput, out float rightInput);
        if (debugLogTimer < Time.fixedDeltaTime * 2f)
            Debug.Log($"[AutonomousReturn] Угол.ошибка: {angleError:F1}°, L: {leftInput:F2}, R: {rightInput:F2}");

        controller.ApplyManualControl(leftInput, rightInput);
    }

    private void CalculateTrackInputs(float angleError, float distance, out float left, out float right)
    {
        float normalizedTurn = Mathf.Clamp(angleError / 90f, -1f, 1f);
        float targetSpeed = Mathf.Clamp(distance * speedGain, 0.5f, maxSpeed);
        float speedCommand = targetSpeed / maxForwardSpeed;
        speedCommand = Mathf.Clamp01(speedCommand);

        left = Mathf.Clamp(speedCommand + normalizedTurn * steeringGain, -1f, 1f);
        right = Mathf.Clamp(speedCommand - normalizedTurn * steeringGain, -1f, 1f);
    }

    private float maxForwardSpeed => controller != null ? controller.GetMaxForwardSpeed() : 6f;

    private void CheckStuck()
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed < stuckSpeedThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckTimeThreshold)
            {
                Debug.Log("[AutonomousReturn] ⚠ Застревание! Запуск процедуры выезда...");
                StartUnstuck();
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void StartUnstuck()
    {
        isUnstucking = true;
        unstuckTimer = 0f;
        unstuckDirection = 1f;
    }

    private void UnstuckRoutine()
    {
        unstuckTimer += Time.fixedDeltaTime;

        if (unstuckTimer < 1f)
            controller.ApplyManualControl(-unstuckDirection * 0.5f, -unstuckDirection * 0.5f);
        else if (unstuckTimer < 2f)
            controller.ApplyManualControl(unstuckDirection * 0.5f, -unstuckDirection * 0.5f);
        else
        {
            isUnstucking = false;
            stuckTimer = 0f;
            Debug.Log("[AutonomousReturn] Процедура выезда завершена.");
        }
    }

    private void OnReturnPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("[AutonomousReturn] Кнопка возврата нажата!");
        StartReturn(automatic: false);
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("[AutonomousReturn] Кнопка отмены нажата!");
        CancelReturn();
    }

    public void StartReturn(bool automatic)
    {
        if (isReturning) return;

        Debug.Log($"[AutonomousReturn] ЗАПУСК ВОЗВРАТА (авто: {automatic}). База: {basePoint.position}, позиция: {transform.position}");

        isReturning = true;
        signalLost = automatic;
        isUnstucking = false;
        stuckTimer = 0f;
        debugLogTimer = 0f;

        // Отключаем ручное управление, чтобы не перезаписывало команды
        if (controller != null)
            controller.enabled = false;

        if (automatic) OnSignalLostEvent?.Invoke();
        OnReturnStarted?.Invoke();
    }

    public void CancelReturn()
    {
        if (!isReturning) return;

        Debug.Log("[AutonomousReturn] ОТМЕНА ВОЗВРАТА");

        isReturning = false;
        signalLost = false;
        isUnstucking = false;

        if (controller != null)
        {
            controller.ApplyManualControl(0f, 0f);
            controller.enabled = true;   // восстанавливаем ручное управление
        }

        OnReturnCancelled?.Invoke();
        if (signalLost) OnSignalRestored?.Invoke();
    }

    private void ArriveAtBase()
    {
        Debug.Log("[AutonomousReturn] ✓✓✓ ПРИБЫТИЕ НА БАЗУ ✓✓✓");

        isReturning = false;
        signalLost = false;
        isUnstucking = false;

        if (controller != null)
        {
            controller.ApplyManualControl(0f, 0f);
            controller.enabled = true;   // восстанавливаем ручное управление
        }

        OnBaseReached?.Invoke();
        if (signalLost) OnSignalRestored?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        if (basePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(basePoint.position, signalRange);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(basePoint.position, arrivalDistance);
        }
    }
}