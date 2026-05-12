using UnityEngine;

[CreateAssetMenu(fileName = "TutorialStep", menuName = "Tutorial/Step", order = 1)]
public class TutorialStep : ScriptableObject
{
    public string stepId;

    public enum StepType
    {
        // Раздельное управление гусеницами
        PressLeftForward,      // Нажать W (левая вперёд)
        PressLeftBackward,     // Нажать S (левая назад)
        PressRightForward,     // Нажать O (правая вперёд)
        PressRightBackward,    // Нажать L (правая назад)

        // Движение
        MoveForward,           // Зажать W + O (прямо)
        MoveBackward,          // Зажать S + L (назад)
        RotateRight,           // W + L (разворот вправо)
        RotateLeft,            // S + O (разворот влево)

        // Системы робота
        ToggleNightVision,     // Нажать N
        ToggleControlMode,     // Нажать M

        // Задания
        AchieveSpeed,          // Разогнаться до скорости
        DriveIntoGas,          // Заехать в зону газа
        ReturnToBase,          // Запустить автовозврат
        DriveDistance          // Проехать дистанцию
    }

    public StepType type;

    [TextArea(3, 10)]
    public string instructionText;

    [Header("Дополнительная информация")]
    public string keyHint;                // "W", "O", "W+O" и т.д.
    public Sprite keyIcon;                // Иконка клавиши (опционально)

    [Header("Параметры для заданий")]
    public float targetSpeed = 10f;
    public float targetDistance = 20f;

    [Header("Сообщение при выполнении")]
    public string completionMessage = "Готово";
}