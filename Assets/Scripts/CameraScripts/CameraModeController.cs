using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using TMPro;

public class CameraModeController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume targetVolume; 

    [Header("Профили")]
    [SerializeField] private VolumeProfile defaultProfile;
    [SerializeField] private VolumeProfile nightProfile;

    [Header("Управление")]
    [SerializeField] private InputActionReference toggleAction;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI modeText;

    private enum Mode { Default, Night, Thermal }
    private Mode currentMode = Mode.Default;

    private void Awake()
    {
        if (targetVolume == null)
            targetVolume = GetComponent<Volume>();

        if (targetVolume == null)
        {
            Debug.LogError("CameraModeController: не назначен Volume!");
            enabled = false;
        }
    }

    private void OnEnable() => toggleAction.action.performed += OnToggle;
    private void OnDisable() => toggleAction.action.performed -= OnToggle;

    private void Start()
    {
        SetMode(Mode.Default);
    }

    private void OnToggle(InputAction.CallbackContext ctx)
    {
        currentMode = (Mode)(((int)currentMode + 1) % 2);
        SetMode(currentMode);
    }

    private void SetMode(Mode mode)
    {
        currentMode = mode;

        switch (mode)
        {
            case Mode.Default:
                targetVolume.profile = defaultProfile;
                break;
            case Mode.Night:
                targetVolume.profile = nightProfile;
                break;
        }

        if (modeText != null)
        {
            modeText.text = mode switch
            {
                Mode.Default => "Режим: Обычный",
                Mode.Night => "Режим: ПНВ",
                _ => ""
            };
        }
    }

    public string CurrentModeName => currentMode switch
    {
        Mode.Default => "Обычный",
        Mode.Night => "ПНВ",
        _ => "Неизвестно"
    };
}