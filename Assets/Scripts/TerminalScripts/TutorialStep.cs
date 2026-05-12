using UnityEngine;

[CreateAssetMenu(fileName = "TutorialStep", menuName = "Tutorial/Step", order = 1)]
public class TutorialStep : ScriptableObject
{
    public string stepId;

    public enum StepType
    {
        MoveForward,
        MoveBackward,
        RotateOnSpot,
        ToggleNightVisionMode,  // Переключение режима камеры на ПНВ
        AchieveSpeed,
        DriveIntoGas,
        ReturnToBase,
        DriveDistance
    }

    public StepType type;

    [TextArea(3, 10)]
    public string instructionText;

    public float targetSpeed = 10f;
    public float targetDistance = 20f;
    public string completionMessage = "Готово ✓";
}