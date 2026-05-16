using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OperatorUI : MonoBehaviour
{
    [Header("Главная панель")]
    [SerializeField] private GameObject mainPanel;

    [Header("Скорость")]
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("Сигнал связи")]
    [SerializeField] private TextMeshProUGUI signalText;

    [Header("Газоанализатор")]
    [SerializeField] private TextMeshProUGUI gasStatusText;
    [SerializeField] private TextMeshProUGUI gasTypeText;
    [SerializeField] private TextMeshProUGUI gasConcentrationText;

    [Header("Режим камеры")]
    [SerializeField] private TextMeshProUGUI cameraModeText;

    [Header("Обучение")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialInstructionText;
    [SerializeField] private TextMeshProUGUI tutorialKeyHintText;
    [SerializeField] private TextMeshProUGUI tutorialStatusText;

    [Header("Ссылки на системы")]
    [SerializeField] private Rigidbody robotRb;
    [SerializeField] private AutonomousReturn autoReturn;
    [SerializeField] private CameraModeController cameraModeController;
    [SerializeField] private GasAnalyzer gasAnalyzer;
    [SerializeField] private TerminalManager terminalManager;

    private bool isVisible = false;

    private void Update()
    {
        if (!isVisible) return;

        UpdateSpeed();
        UpdateSignal();
        UpdateGasDetector();
        UpdateCameraMode();
    }

    private void UpdateSpeed()
    {
        if (robotRb == null) return;

        float speedKMH = robotRb.linearVelocity.magnitude * 3.6f;

        if (speedText != null)
            speedText.text = $"{speedKMH:F1} км/ч";
    }

    private void UpdateSignal()
    {
        if (autoReturn == null) return;

        float signal = autoReturn.SignalStrength;

        if (signalText != null)
        {
            int pct = Mathf.RoundToInt(signal * 100f);
            signalText.text = $"Сигнал: {pct}%";

            if (signal > 0.7f)
                signalText.color = Color.green;
            else if (signal > 0.3f)
                signalText.color = Color.yellow;
            else
                signalText.color = Color.red;
        }

    }

    private void UpdateGasDetector()
    {
        if (gasAnalyzer == null) return;

        bool inGas = gasAnalyzer.IsInGasZone;
        float concentration = gasAnalyzer.CurrentConcentration;

        if (gasStatusText != null)
        {
            if (inGas)
            {
                gasStatusText.text = "Обнаружено";
                gasStatusText.color = Color.red;
            }
            else
            {
                gasStatusText.text = "Воздух чист";
                gasStatusText.color = Color.green;
            }
        }

        if (gasTypeText != null)
        {
            if (inGas && gasAnalyzer.CurrentGasType.HasValue)
            {
                gasTypeText.text = $"Тип: {GetGasName(gasAnalyzer.CurrentGasType.Value)}";
            }
            else
            {
                gasTypeText.text = "";
            }
        }

        if (gasConcentrationText != null)
        {
            if (inGas)
                gasConcentrationText.text = $"Конц: {concentration * 100f:F0}%";
            else
                gasConcentrationText.text = "";
        }
    }

    private void UpdateCameraMode()
    {
        if (cameraModeController == null || cameraModeText == null) return;
        cameraModeText.text = $"Режим: {cameraModeController.CurrentModeName}";
    }

    public void Show()
    {
        isVisible = true;
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void Hide()
    {
        isVisible = false;
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    public void ShowTutorialStep(string instruction, string status, string keyHint = "")
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        if (tutorialInstructionText != null)
            tutorialInstructionText.text = instruction;

        if (tutorialKeyHintText != null)
        {
            if (!string.IsNullOrEmpty(keyHint))
            {
                tutorialKeyHintText.text = $"[ Клавиши: {keyHint} ]";
                tutorialKeyHintText.gameObject.SetActive(true);
            }
            else
            {
                tutorialKeyHintText.gameObject.SetActive(false);
            }
        }

        if (tutorialStatusText != null)
            tutorialStatusText.text = status;
    }

    public void HideTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    public void UpdateTutorialStatus(string status)
    {
        if (tutorialStatusText != null)
            tutorialStatusText.text = status;
    }

    private string GetGasName(GasType type) => type switch
    {
        GasType.Chlorine => "Хлор",
        GasType.Ammonia => "Аммиак",
        GasType.HydrogenSulfide => "Сероводород",
        _ => "Неизвестный"
    };
}