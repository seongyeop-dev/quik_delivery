using UnityEngine;

public class SettingsRepository : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private JsonFileService jsonFileService;

    [Header("Storage Settings")]
    [SerializeField] private string fileName = "app_settings.json";

    private void Awake()
    {
        TryInitializeDependencies();
    }

    /// <summary>
    /// 외부 참조가 비어 있으면 자동 탐색
    /// </summary>
    private void TryInitializeDependencies()
    {
        if (jsonFileService == null)
        {
            jsonFileService = FindFirstObjectByType<JsonFileService>();
        }
    }

    /// <summary>
    /// 저장된 앱 설정 불러오기
    /// 파일이 없으면 기본 AppSettings 반환
    /// </summary>
    public AppSettings LoadSettings()
    {
        TryInitializeDependencies();

        if (jsonFileService == null)
        {
            Debug.LogWarning("[SettingsRepository] JsonFileService 참조가 없습니다.");
            return new AppSettings();
        }

        AppSettings settings = jsonFileService.LoadFromJson<AppSettings>(fileName);

        if (settings == null)
        {
            return new AppSettings();
        }

        return settings;
    }

    /// <summary>
    /// 앱 설정 저장
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        TryInitializeDependencies();

        if (jsonFileService == null)
        {
            Debug.LogWarning("[SettingsRepository] JsonFileService 참조가 없습니다.");
            return;
        }

        if (settings == null)
        {
            Debug.LogWarning("[SettingsRepository] 저장할 settings가 null입니다.");
            return;
        }

        jsonFileService.SaveToJson(fileName, settings);
        Debug.Log("[SettingsRepository] 설정 저장 완료");
    }

    /// <summary>
    /// 기본 설정으로 초기화
    /// </summary>
    public void ResetSettings()
    {
        SaveSettings(new AppSettings());
    }
}