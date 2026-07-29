using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SettingsRepository settingsRepository;

    [Header("Setting Controls")]
    [SerializeField] private Toggle toggleUseLocationAutoFill;
    [SerializeField] private Toggle toggleUseLargeFontMode;
    [SerializeField] private Toggle toggleConfirmBeforeDelete;

    [Header("Status UI")]
    [SerializeField] private TMP_Text txtSaveStatus;

    [Header("Buttons")]
    [SerializeField] private Button btnSaveSettings;

    private AppSettings currentSettings;

    private void Awake()
    {
        TryInitializeDependencies();
        RegisterEvents();
        LoadSettingsToUI();
    }

    private void OnEnable()
    {
        LoadSettingsToUI();
    }

    private void TryInitializeDependencies()
    {
        if (settingsRepository == null)
        {
            settingsRepository = FindFirstObjectByType<SettingsRepository>();
        }
    }

    private void RegisterEvents()
    {
        if (btnSaveSettings != null)
        {
            btnSaveSettings.onClick.AddListener(OnClickSaveSettings);
        }
    }

    private void LoadSettingsToUI()
    {
        TryInitializeDependencies();

        if (settingsRepository == null)
        {
            Debug.LogWarning("[SettingsPanelUI] SettingsRepository 참조가 없습니다.");
            return;
        }

        currentSettings = settingsRepository.LoadSettings();

        if (currentSettings == null)
        {
            currentSettings = new AppSettings();
        }

        if (toggleUseLocationAutoFill != null)
        {
            toggleUseLocationAutoFill.isOn = currentSettings.UseLocationAutoFill;
        }

        if (toggleUseLargeFontMode != null)
        {
            toggleUseLargeFontMode.isOn = currentSettings.UseLargeFontMode;
        }

        if (toggleConfirmBeforeDelete != null)
        {
            toggleConfirmBeforeDelete.isOn = currentSettings.ConfirmBeforeDelete;
        }

        if (txtSaveStatus != null)
        {
            txtSaveStatus.text = "현재 저장된 설정을 불러왔습니다";
        }
    }

    private AppSettings BuildSettingsFromUI()
    {
        AppSettings settings = currentSettings ?? new AppSettings();

        settings.UseLocationAutoFill =
            toggleUseLocationAutoFill != null && toggleUseLocationAutoFill.isOn;

        settings.UseLargeFontMode =
            toggleUseLargeFontMode != null && toggleUseLargeFontMode.isOn;

        settings.ConfirmBeforeDelete =
            toggleConfirmBeforeDelete != null && toggleConfirmBeforeDelete.isOn;

        return settings;
    }

    private void OnClickSaveSettings()
    {
        TryInitializeDependencies();

        if (settingsRepository == null)
        {
            Debug.LogWarning("[SettingsPanelUI] SettingsRepository 참조가 없습니다.");
            return;
        }

        AppSettings newSettings = BuildSettingsFromUI();
        settingsRepository.SaveSettings(newSettings);
        currentSettings = newSettings;

        if (txtSaveStatus != null)
        {
            txtSaveStatus.text = "설정을 저장했습니다";
        }

        Debug.Log("[SettingsPanelUI] 설정 저장 완료");
    }
}