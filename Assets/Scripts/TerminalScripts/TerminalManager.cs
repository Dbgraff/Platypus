using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerminalManager : MonoBehaviour
{
    [Header("Камеры")]
    [SerializeField] private Camera terminalCamera;    // камера перед монитором
    [SerializeField] private Camera robotCamera;       // камера на роботе

    [Header("Панели меню (на мониторе)")]
    [SerializeField] private GameObject mainMenuPanel;   // главное меню
    [SerializeField] private CanvasGroup mainMenuCanvasGroup; // для плавного появления
    //[SerializeField] private GameObject settingsPanel;
    //[SerializeField] private GameObject trainingPanel;
    //[SerializeField] private GameObject creditsPanel;

    [Header("Кнопки главного меню")]
    [SerializeField] private Button freePlayButton;
    [SerializeField] private Button trainingButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Кнопки настроек")]
    [SerializeField] private Button backFromSettingsButton;

    [Header("Кнопки обучения")]
    [SerializeField] private Button skipTrainingButton;

    [Header("Эффекты экрана")]
    [SerializeField] private Material screenMaterial;      // материал с CRT-шейдером
    [SerializeField] private GameObject noiseOverlay;      // объект шума на экране
    [SerializeField] private float bootDelay = 0.3f;       // начальная задержка
    [SerializeField] private float bootDuration = 1.5f;    // длительность включения
    [SerializeField] private float menuFadeDuration = 0.8f; // длительность появления меню

    [Header("Системы игры")]
    [SerializeField] private GameObject robot;             // корневой объект робота
    [SerializeField] private OperatorUI operatorUI;        // интерфейс оператора
    [SerializeField] private TutorialManager tutorialManager; // менеджер обучения
    [SerializeField] private AutoReturnToBase autoReturn;  // система автовозврата

    // Приватные переменные
    private bool robotActive = false;
    private bool isBooting = true;

    private void Start()
    {
        // Начальное состояние
        SetRobotActive(false);                  // робот выключен

        // Настраиваем панель главного меню
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);      // панель активна, но прозрачна
            if (mainMenuCanvasGroup == null)
            {
                // Если забыли добавить CanvasGroup, добавляем автоматически
                mainMenuCanvasGroup = mainMenuPanel.GetComponent<CanvasGroup>();
                if (mainMenuCanvasGroup == null)
                    mainMenuCanvasGroup = mainMenuPanel.AddComponent<CanvasGroup>();
            }
            mainMenuCanvasGroup.alpha = 0f;     // полностью прозрачно
            mainMenuCanvasGroup.interactable = false; // кнопки не нажимаются
            mainMenuCanvasGroup.blocksRaycasts = false;
        }

        // Подписываемся на кнопки
        freePlayButton.onClick.AddListener(OnFreePlayClicked);
        //trainingButton.onClick.AddListener(OnTrainingClicked);
        //settingsButton.onClick.AddListener(OpenSettings);
        //quitButton.onClick.AddListener(QuitGame);
        //backFromSettingsButton.onClick.AddListener(CloseSettings);
        //if (skipTrainingButton != null) skipTrainingButton.onClick.AddListener(SkipTraining);

        // Запускаем анимацию включения монитора
        StartCoroutine(BootSequence());
    }

    /// <summary>
    /// Анимация включения монитора: шум, постепенное повышение яркости, затем плавное появление меню.
    /// </summary>
    private IEnumerator BootSequence()
    {
        isBooting = true;

        // Начальное состояние - экран выключен
        if (screenMaterial != null)
        {
            screenMaterial.SetFloat("_Brightness", 0f);
        }
        if (noiseOverlay != null)
        {
            noiseOverlay.SetActive(false);
        }

        yield return new WaitForSeconds(bootDelay);

        // Включаем шум на экране
        if (noiseOverlay != null)
        {
            noiseOverlay.SetActive(true);
        }

        yield return new WaitForSeconds(0.2f);

        // Плавно повышаем яркость экрана
        float elapsed = 0f;
        while (elapsed < bootDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bootDuration;
            // Используем нелинейную кривую для более естественного включения
            t = Mathf.SmoothStep(0f, 1f, t);

            if (screenMaterial != null)
            {
                screenMaterial.SetFloat("_Brightness", t);
            }
            yield return null;
        }

        // Экран полностью включен
        if (screenMaterial != null)
        {
            screenMaterial.SetFloat("_Brightness", 1f);
        }

        // Убираем шум
        if (noiseOverlay != null)
        {
            noiseOverlay.SetActive(false);
        }

        yield return new WaitForSeconds(0.3f);

        // ПЛАВНОЕ появление главного меню
        yield return StartCoroutine(FadeInMenu());

        isBooting = false;
    }

    /// <summary>
    /// Плавное появление панели главного меню.
    /// </summary>
    private IEnumerator FadeInMenu()
    {
        if (mainMenuCanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = mainMenuCanvasGroup.alpha;

        while (elapsed < menuFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / menuFadeDuration;
            // Используем SmoothStep для более приятной анимации
            t = Mathf.SmoothStep(0f, 1f, t);

            mainMenuCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }

        // Финальное состояние
        mainMenuCanvasGroup.alpha = 1f;
        mainMenuCanvasGroup.interactable = true;
        mainMenuCanvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// Плавное скрытие панели главного меню.
    /// </summary>
    private IEnumerator FadeOutMenu()
    {
        if (mainMenuCanvasGroup == null) yield break;

        mainMenuCanvasGroup.interactable = false;
        mainMenuCanvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        float startAlpha = mainMenuCanvasGroup.alpha;

        while (elapsed < menuFadeDuration * 0.5f) // быстрее чем появление
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (menuFadeDuration * 0.5f);
            t = Mathf.SmoothStep(0f, 1f, t);

            mainMenuCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        mainMenuCanvasGroup.alpha = 0f;
        mainMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Включает или выключает робота и соответствующие системы.
    /// </summary>
    private void SetRobotActive(bool active)
    {
        robotActive = active;

        // Робот и его компоненты
        if (robot != null)
        {
            robot.SetActive(active);
        }

        // Камера робота
        if (robotCamera != null)
        {
            robotCamera.gameObject.SetActive(active);
        }

        // Включаем или выключаем управление роботом
        if (robot != null)
        {
            var controller = robot.GetComponent<RobotController>();
            if (controller != null)
            {
                controller.enabled = active;
            }
        }

        // Интерфейс оператора
        if (operatorUI != null)
        {
            operatorUI.gameObject.SetActive(active);
        }

        // Камера терминала (наоборот)
        if (terminalCamera != null)
        {
            terminalCamera.gameObject.SetActive(!active);
        }

        // Курсор
        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !active;
    }

    /// <summary>
    /// Обработчик кнопки "Свободная игра".
    /// </summary>
    private void OnFreePlayClicked()
    {
        if (isBooting) return;

        StartCoroutine(LaunchFreePlay());
    }

    private IEnumerator LaunchFreePlay()
    {
        // Плавно скрываем меню
        yield return StartCoroutine(FadeOutMenu());

        // Включаем робота
        SetRobotActive(true);

        if (operatorUI != null)
        {
            operatorUI.Show();
        }

        Debug.Log("Свободная игра запущена");
    }

    /// <summary>
    /// Возврат в главное меню из игры.
    /// </summary>
    public void ReturnToMenu()
    {
        // Если робот в процессе возврата - отменяем
        if (autoReturn != null && autoReturn.IsReturning)
        {
            autoReturn.CancelReturn();
        }

        // Отключаем робота
        SetRobotActive(false);

        if (operatorUI != null)
        {
            operatorUI.Hide();
        }

        // Запускаем плавный переход
        StartCoroutine(TransitionToMenu());
    }

    private IEnumerator TransitionToMenu()
    {
        // Затемняем экран
        if (screenMaterial != null)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                screenMaterial.SetFloat("_Brightness", Mathf.Lerp(1f, 0f, t));
                yield return null;
            }
            screenMaterial.SetFloat("_Brightness", 0f);
        }

        yield return new WaitForSeconds(0.2f);

        // Показываем панель меню (но прозрачной)
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

        // Включаем экран обратно
        if (screenMaterial != null)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                screenMaterial.SetFloat("_Brightness", Mathf.Lerp(0f, 1f, t));
                yield return null;
            }
            screenMaterial.SetFloat("_Brightness", 1f);
        }

        // Плавно показываем меню
        yield return StartCoroutine(FadeInMenu());
    }

    /// <summary>
    /// Выход из игры.
    /// </summary>
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
        //trainingButton.onClick.RemoveListener(OnTrainingClicked);
        //settingsButton.onClick.RemoveListener(OpenSettings);
        //quitButton.onClick.RemoveListener(QuitGame);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnValidate()
    {
        if (bootDuration < 0.1f) bootDuration = 0.1f;
        if (menuFadeDuration < 0.1f) menuFadeDuration = 0.1f;
    }
}