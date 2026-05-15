using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerminalManager : MonoBehaviour
{
    [Header("Камеры")]
    [SerializeField] private Camera terminalCamera;
    [SerializeField] private Camera robotCamera;

    [Header("Панели меню (на мониторе)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;

    [Header("Кнопки главного меню")]
    [SerializeField] private Button freePlayButton;
    [SerializeField] private Button trainingButton;
    [SerializeField] private Button quitButton;

    [Header("Эффекты экрана")]
    [SerializeField] private Material screenMaterial;
    [SerializeField] private GameObject noiseOverlay;
    [SerializeField] private float bootDelay = 0.3f;
    [SerializeField] private float bootDuration = 1.5f;
    [SerializeField] private float menuFadeDuration = 0.8f;

    [Header("Системы игры")]
    [SerializeField] private GameObject robot;
    [SerializeField] private OperatorUI operatorUI;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private AutoReturnToBase autoReturn;

    private bool robotActive = false;
    private bool isBooting = true;

    private void Start()
    {
        SetRobotActive(false);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            if (mainMenuCanvasGroup == null)
            {
                mainMenuCanvasGroup = mainMenuPanel.GetComponent<CanvasGroup>();
                if (mainMenuCanvasGroup == null)
                    mainMenuCanvasGroup = mainMenuPanel.AddComponent<CanvasGroup>();
            }
            mainMenuCanvasGroup.alpha = 0f;
            mainMenuCanvasGroup.interactable = false;
            mainMenuCanvasGroup.blocksRaycasts = false;
        }

        freePlayButton.onClick.AddListener(OnFreePlayClicked);
        trainingButton.onClick.AddListener(OnTrainingClicked);
        quitButton.onClick.AddListener(QuitGame);

        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        isBooting = true;

        if (screenMaterial != null)
            screenMaterial.SetFloat("_Brightness", 0f);
        if (noiseOverlay != null)
            noiseOverlay.SetActive(false);

        yield return new WaitForSeconds(bootDelay);

        if (noiseOverlay != null)
            noiseOverlay.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        float elapsed = 0f;
        while (elapsed < bootDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / bootDuration);
            if (screenMaterial != null)
                screenMaterial.SetFloat("_Brightness", t);
            yield return null;
        }

        if (screenMaterial != null)
            screenMaterial.SetFloat("_Brightness", 1f);

        if (noiseOverlay != null)
            noiseOverlay.SetActive(false);

        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(FadeInMenu());
        isBooting = false;
    }

    private IEnumerator FadeInMenu()
    {
        if (mainMenuCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < menuFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / menuFadeDuration);
            mainMenuCanvasGroup.alpha = t;
            yield return null;
        }
        mainMenuCanvasGroup.alpha = 1f;
        mainMenuCanvasGroup.interactable = true;
        mainMenuCanvasGroup.blocksRaycasts = true;
    }

    private IEnumerator FadeOutMenu()
    {
        if (mainMenuCanvasGroup == null)
        {
            mainMenuPanel.SetActive(false);
            yield break;
        }

        mainMenuCanvasGroup.interactable = false;
        mainMenuCanvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        float duration = menuFadeDuration * 0.5f;
        float startAlpha = mainMenuCanvasGroup.alpha;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            mainMenuCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        mainMenuCanvasGroup.alpha = 0f;
        mainMenuPanel.SetActive(false);
    }

    private void SetRobotActive(bool active)
    {
        robotActive = active;

        if (robot != null)
            robot.SetActive(active);

        if (robotCamera != null)
            robotCamera.gameObject.SetActive(active);

        if (robot != null)
        {
            var controller = robot.GetComponent<RobotController>();
            if (controller != null)
                controller.enabled = active;
        }

        if (operatorUI != null)
            operatorUI.gameObject.SetActive(active);

        if (terminalCamera != null)
            terminalCamera.gameObject.SetActive(!active);

        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !active;
    }

    private void OnFreePlayClicked()
    {
        if (isBooting) return;
        StartCoroutine(LaunchFreePlay());
    }

    private IEnumerator LaunchFreePlay()
    {
        yield return StartCoroutine(FadeOutMenu());
        SetRobotActive(true);
        if (operatorUI != null) operatorUI.Show();
        Debug.Log("Свободная игра запущена");
    }

    private void OnTrainingClicked()
    {
        if (isBooting) return;
        StartCoroutine(LaunchTraining());
    }

    private IEnumerator LaunchTraining()
    {
        yield return StartCoroutine(FadeOutMenu());
        SetRobotActive(true);
        if (operatorUI != null) operatorUI.Show();
        if (tutorialManager != null) tutorialManager.StartTraining();
        Debug.Log("Обучение запущено");
    }

    public void ReturnToMenu()
    {
        if (autoReturn != null && autoReturn.IsReturning)
            autoReturn.CancelReturn();

        if (tutorialManager != null && tutorialManager.IsTraining)
            tutorialManager.StopTraining();

        SetRobotActive(false);

        if (operatorUI != null)
            operatorUI.Hide();

        StartCoroutine(TransitionToMenu());
    }

    private IEnumerator TransitionToMenu()
    {
        if (screenMaterial != null)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                screenMaterial.SetFloat("_Brightness", Mathf.Lerp(1f, 0f, elapsed / duration));
                yield return null;
            }
            screenMaterial.SetFloat("_Brightness", 0f);
        }

        yield return new WaitForSeconds(0.2f);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = 0f;
                mainMenuCanvasGroup.interactable = false;
                mainMenuCanvasGroup.blocksRaycasts = false;
            }
        }

        if (screenMaterial != null)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                screenMaterial.SetFloat("_Brightness", Mathf.Lerp(0f, 1f, elapsed / duration));
                yield return null;
            }
            screenMaterial.SetFloat("_Brightness", 1f);
        }

        yield return StartCoroutine(FadeInMenu());
    }

    private void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        freePlayButton.onClick.RemoveListener(OnFreePlayClicked);
        trainingButton.onClick.RemoveListener(OnTrainingClicked);
        quitButton.onClick.RemoveListener(QuitGame);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnValidate()
    {
        if (bootDuration < 0.1f) bootDuration = 0.1f;
        if (menuFadeDuration < 0.1f) menuFadeDuration = 0.1f;
    }
}