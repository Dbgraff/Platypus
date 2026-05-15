using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OperatorUI : MonoBehaviour
{
    [Header("Главная панель")]
    [SerializeField] private GameObject mainPanel;

    [Header("Скорость")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private Image speedFillBar;

    [Header("Сигнал связи")]
    [SerializeField] private TextMeshProUGUI signalText;
    [SerializeField] private Image signalBar;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Газоанализатор")]
    [SerializeField] private GameObject gasWarningPanel;
    [SerializeField] private TextMeshProUGUI gasStatusText;
    [SerializeField] private TextMeshProUGUI gasTypeText;
    [SerializeField] private TextMeshProUGUI gasConcentrationText;

    [Header("Режим камеры")]
    [SerializeField] private TextMeshProUGUI cameraModeText;

    [Header("Автовозврат")]
    [SerializeField] private TextMeshProUGUI returnStatusText;
    [SerializeField] private GameObject returnWarningPanel;

    [Header("Обучение")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialInstructionText;
    [SerializeField] private TextMeshProUGUI tutorialKeyHintText;
    [SerializeField] private TextMeshProUGUI tutorialStatusText;

    [Header("Ссылки на системы")]
    [SerializeField] private Rigidbody robotRb;
    [SerializeField] private AutoReturnToBase autoReturn;
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
        UpdateReturnStatus();
    }

    // ===========================
    // Обновление данных
    // ===========================

    private void UpdateSpeed()
    {
        if (robotRb == null) return;

        float speedKMH = robotRb.linearVelocity.magnitude * 3.6f;

        if (speedText != null)
            speedText.text = $"{speedKMH:F1} км/ч";

        if (speedFillBar != null)
            speedFillBar.fillAmount = Mathf.Clamp01(speedKMH / 15f);
    }

    private void UpdateSignal()
    {
        if (autoReturn == null) return;

        float distance = autoReturn.DistanceToBase;
        float signal = autoReturn.SignalStrength;

        if (distanceText != null)
            distanceText.text = $"Дальность: {distance:F0} м";

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

        if (signalBar != null)
        {
            signalBar.fillAmount = signal;
            if (signal > 0.7f) signalBar.color = Color.green;
            else if (signal > 0.3f) signalBar.color = Color.yellow;
            else signalBar.color = Color.red;
        }
    }

    private void UpdateGasDetector()
    {
        if (gasAnalyzer == null) return;

        bool inGas = gasAnalyzer.IsInGasZone;
        float concentration = gasAnalyzer.CurrentConcentration;

        if (gasWarningPanel != null)
            gasWarningPanel.SetActive(inGas);

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

    private void UpdateReturnStatus()
    {
        if (autoReturn == null) return;

        bool isReturning = autoReturn.IsReturning;
        bool signalLost = autoReturn.SignalLost;

        if (returnWarningPanel != null)
            returnWarningPanel.SetActive(isReturning);

        if (returnStatusText != null)
        {
            if (signalLost)
            {
                returnStatusText.text = "ПОТЕРЯ СВЯЗИ\nАвтовозврат...";
                returnStatusText.color = Color.red;
            }
            else if (isReturning)
            {
                returnStatusText.text = "Автовозврат...";
                returnStatusText.color = Color.yellow;
            }
            else
            {
                returnStatusText.text = "";
            }
        }
    }

    // ===========================
    // Управление видимостью
    // ===========================

    public void Show()
    {
        Debug.Log($"[OperatorUI] Show вызван. mainPanel: {(mainPanel != null ? mainPanel.name : "NULL")}, текущий activeSelf: {(mainPanel != null ? mainPanel.activeSelf.ToString() : "—")}");
        isVisible = true;
        mainPanel.SetActive(true);
    }

    public void Hide()
    {
        isVisible = false;
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    // ===========================
    // Для обучения
    // ===========================

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

    // ===========================
    // Вспомогательные
    // ===========================

    private string GetGasName(GasType type) => type switch
    {
        GasType.Chlorine => "Хлор",
        GasType.Ammonia => "Аммиак",
        GasType.HydrogenSulfide => "Сероводород",
        _ => "Неизвестный"
    };
}