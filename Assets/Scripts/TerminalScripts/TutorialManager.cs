using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    [Header("Шаги обучения")]
    [SerializeField] private TutorialStep[] steps;

    [Header("Ссылки")]
    [SerializeField] private OperatorUI operatorUI;
    [SerializeField] private RobotController robotController;
    [SerializeField] private CameraModeController cameraModeController;
    [SerializeField] private GasAnalyzer gasAnalyzer;
    [SerializeField] private AutoReturnToBase autoReturn;
    [SerializeField] private Rigidbody robotRb;

    [Header("Настройки")]
    [SerializeField] private float stepCompletionDelay = 1.5f;

    // Состояние
    private int currentStepIndex = 0;
    private bool isTraining = false;
    private Dictionary<string, bool> stepCompleted = new Dictionary<string, bool>();
    private Vector3 stepStartPosition;

    // Свойства
    public bool IsTraining => isTraining;
    public int CurrentStepIndex => currentStepIndex;
    public int TotalSteps => steps != null ? steps.Length : 0;

    // Событие завершения обучения
    public event Action OnTrainingComplete;

    private void Start()
    {
        enabled = false;
    }

    public void StartTraining()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("TutorialManager: нет шагов обучения!");
            operatorUI?.ShowTutorialStep("Обучение не настроено.", "");
            return;
        }

        isTraining = true;
        currentStepIndex = 0;
        stepCompleted.Clear();

        for (int i = 0; i < steps.Length; i++)
            stepCompleted[steps[i].stepId] = false;

        enabled = true;
        ShowCurrentStep();
    }

    public void StopTraining()
    {
        isTraining = false;
        currentStepIndex = 0;
        stepCompleted.Clear();
        enabled = false;
        operatorUI?.HideTutorial();
    }

    private void Update()
    {
        if (!isTraining || currentStepIndex >= steps.Length) return;

        TutorialStep step = steps[currentStepIndex];
        bool completed = CheckStep(step);

        if (completed)
        {
            MarkStepCompleted(step.stepId);
            // Блокируем повторные проверки
            enabled = false;
            StartCoroutine(AdvanceToNextStep());
        }
    }

    // ===========================
    // Проверка шагов
    // ===========================

    private bool CheckStep(TutorialStep step)
    {
        return step.type switch
        {
            TutorialStep.StepType.MoveForward => CheckMoveForward(),
            TutorialStep.StepType.MoveBackward => CheckMoveBackward(),
            TutorialStep.StepType.RotateOnSpot => CheckRotateOnSpot(),
            TutorialStep.StepType.ToggleNightVisionMode => CheckNightVisionMode(),
            TutorialStep.StepType.AchieveSpeed => CheckAchieveSpeed(step),
            TutorialStep.StepType.DriveIntoGas => CheckDriveIntoGas(),
            TutorialStep.StepType.ReturnToBase => CheckReturnToBase(),
            TutorialStep.StepType.DriveDistance => CheckDriveDistance(step),
            _ => false
        };
    }

    private bool CheckMoveForward() => Input.GetKey(KeyCode.W);
    private bool CheckMoveBackward() => Input.GetKey(KeyCode.S);

    private bool CheckRotateOnSpot()
    {
        return (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            && !Input.GetKey(KeyCode.W)
            && !Input.GetKey(KeyCode.S);
    }

    private bool CheckNightVisionMode()
    {
        // Проверяем, что режим камеры переключён на ночной
        return cameraModeController != null
            && cameraModeController.CurrentModeName == "ПНВ";
    }

    private bool CheckAchieveSpeed(TutorialStep step)
    {
        if (robotRb == null) return false;
        return robotRb.linearVelocity.magnitude * 3.6f >= step.targetSpeed;
    }

    private bool CheckDriveIntoGas()
    {
        return gasAnalyzer != null && gasAnalyzer.IsInGasZone;
    }

    private bool CheckReturnToBase()
    {
        return autoReturn != null && autoReturn.IsReturning;
    }

    private bool CheckDriveDistance(TutorialStep step)
    {
        if (robotRb == null) return false;
        return Vector3.Distance(stepStartPosition, robotRb.transform.position) >= step.targetDistance;
    }

    // ===========================
    // Управление шагами
    // ===========================

    private void ShowCurrentStep()
    {
        if (currentStepIndex >= steps.Length) return;

        TutorialStep step = steps[currentStepIndex];
        stepStartPosition = robotRb != null ? robotRb.transform.position : Vector3.zero;

        operatorUI?.ShowTutorialStep(step.instructionText, "");

        Debug.Log($"[Обучение] Шаг {currentStepIndex + 1}/{steps.Length}: {step.instructionText}");
    }

    private void MarkStepCompleted(string stepId)
    {
        if (stepCompleted.ContainsKey(stepId))
            stepCompleted[stepId] = true;
    }

    private IEnumerator AdvanceToNextStep()
    {
        var step = steps[currentStepIndex];
        operatorUI?.UpdateTutorialStatus(step.completionMessage);

        yield return new WaitForSeconds(stepCompletionDelay);

        currentStepIndex++;

        if (currentStepIndex < steps.Length)
        {
            enabled = true;
            ShowCurrentStep();
        }
        else
        {
            CompleteTraining();
        }
    }

    private void CompleteTraining()
    {
        isTraining = false;
        enabled = false;

        operatorUI?.ShowTutorialStep("Обучение пройдено! ✓", "Вы готовы к работе.");
        OnTrainingComplete?.Invoke();

        StartCoroutine(HideCompleteMessage());
    }

    private IEnumerator HideCompleteMessage()
    {
        yield return new WaitForSeconds(3f);
        operatorUI?.HideTutorial();
    }

    // Для отладки
    public void SkipCurrentStep()
    {
        if (!isTraining) return;
        StopAllCoroutines();
        currentStepIndex++;
        if (currentStepIndex >= steps.Length)
            CompleteTraining();
        else
        {
            enabled = true;
            ShowCurrentStep();
        }
    }
}