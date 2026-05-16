using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    [Header("Шаги обучения")]
    [SerializeField] private TutorialStep[] steps;

    [Header("Ссылки")]
    [SerializeField] private OperatorUI operatorUI;
    [SerializeField] private CameraModeController cameraModeController;
    [SerializeField] private GasAnalyzer gasAnalyzer;
    [SerializeField] private AutonomousReturn autoReturn;
    [SerializeField] private Rigidbody robotRb;

    [Header("Настройки")]
    [SerializeField] private float stepCompletionDelay = 1.5f;

    private int currentStepIndex = 0;
    private bool isTraining = false;
    private Vector3 stepStartPosition;
    private Keyboard keyboard;

    public bool IsTraining => isTraining;
    public int CurrentStepIndex => currentStepIndex;
    public int TotalSteps => steps != null ? steps.Length : 0;

    public event Action OnTrainingComplete;

    private void Awake()
    {
        keyboard = Keyboard.current;
    }

    private void Start()
    {
        enabled = false;
    }

    public void StartTraining()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("TutorialManager: нет шагов обучения!");
            operatorUI?.ShowTutorialStep("Обучение не настроено.", "Добавьте шаги в инспекторе.");
            return;
        }

        isTraining = true;
        currentStepIndex = 0;
        enabled = true;
        ShowCurrentStep();
    }

    public void StopTraining()
    {
        isTraining = false;
        currentStepIndex = 0;
        enabled = false;
        operatorUI?.HideTutorial();
    }

    private void Update()
    {
        if (!isTraining || currentStepIndex >= steps.Length) return;
        if (keyboard == null) return;

        TutorialStep step = steps[currentStepIndex];
        bool completed = CheckStep(step);

        if (completed)
        {
            enabled = false;
            StartCoroutine(AdvanceToNextStep());
        }
    }

    private bool CheckStep(TutorialStep step)
    {
        return step.type switch
        {
            TutorialStep.StepType.PressLeftForward => keyboard.wKey.isPressed && !keyboard.oKey.isPressed,
            TutorialStep.StepType.PressLeftBackward => keyboard.sKey.isPressed && !keyboard.lKey.isPressed,
            TutorialStep.StepType.PressRightForward => keyboard.oKey.isPressed && !keyboard.wKey.isPressed,
            TutorialStep.StepType.PressRightBackward => keyboard.lKey.isPressed && !keyboard.sKey.isPressed,

            TutorialStep.StepType.MoveForward => keyboard.wKey.isPressed && keyboard.oKey.isPressed,
            TutorialStep.StepType.MoveBackward => keyboard.sKey.isPressed && keyboard.lKey.isPressed,
            TutorialStep.StepType.RotateRight => keyboard.wKey.isPressed && keyboard.lKey.isPressed,
            TutorialStep.StepType.RotateLeft => keyboard.sKey.isPressed && keyboard.oKey.isPressed,

            TutorialStep.StepType.ToggleNightVision => CheckNightVision(),
            TutorialStep.StepType.ToggleControlMode => keyboard.mKey.wasPressedThisFrame,

            TutorialStep.StepType.AchieveSpeed => CheckAchieveSpeed(step),
            TutorialStep.StepType.DriveIntoGas => CheckDriveIntoGas(),
            TutorialStep.StepType.ReturnToBase => CheckReturnToBase(),
            TutorialStep.StepType.DriveDistance => CheckDriveDistance(step),

            _ => false
        };
    }

    private bool CheckNightVision()
    {
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

    private void ShowCurrentStep()
    {
        if (currentStepIndex >= steps.Length) return;

        TutorialStep step = steps[currentStepIndex];
        stepStartPosition = robotRb != null ? robotRb.transform.position : Vector3.zero;

        operatorUI?.ShowTutorialStep(
            instruction: step.instructionText,
            status: "",
            keyHint: step.keyHint
        );

        Debug.Log($"[Обучение] Шаг {currentStepIndex + 1}/{steps.Length}: {step.instructionText}");
    }

    private IEnumerator AdvanceToNextStep()
    {
        TutorialStep step = steps[currentStepIndex];
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
        operatorUI?.ShowTutorialStep("Обучение пройдено!", "Вы готовы к самостоятельной работе.");
        OnTrainingComplete?.Invoke();
        StartCoroutine(HideCompleteMessage());
    }

    private IEnumerator HideCompleteMessage()
    {
        yield return new WaitForSeconds(3f);
        operatorUI?.HideTutorial();
    }
}