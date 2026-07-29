using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettlementPanelUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private DeliveryManager deliveryManager;
    [SerializeField] private BackupDataManager backupDataManager;

    [Header("Linked UI")]
    [SerializeField] private HomePanelUI homePanelUI;
    [SerializeField] private DeliveryListPanelUI deliveryListPanelUI;
    [SerializeField] private StatsPanelUI statsPanelUI;

    [Header("Year Navigation")]
    [SerializeField] private Button btnPrevYear;
    [SerializeField] private TMP_Text txtYearTitle;
    [SerializeField] private Button btnNextYear;

    [Header("Actions")]
    [SerializeField] private Button btnRefresh;
    [SerializeField] private Button btnBackupToggle;

    [Header("Backup UI")]
    [SerializeField] private GameObject panelBackup;
    [SerializeField] private TMP_Text txtBackupInfo;
    [SerializeField] private Button btnBackupAllData;
    [SerializeField] private Button btnExportBackup;
    [SerializeField] private Button btnImportBackup;

    [Header("Monthly Cards")]
    [SerializeField] private MonthlySummaryCardUI[] monthlyCards = new MonthlySummaryCardUI[12];

    private int selectedYear;

    private void Awake()
    {
        TryInitializeDependencies();
        selectedYear = DateTime.Today.Year;
        RegisterEvents();
        SetBackupPanelVisible(false);
    }

    private void OnEnable()
    {
        if (selectedYear == 0) selectedYear = DateTime.Today.Year;
        RefreshYear(selectedYear);
    }

    private void TryInitializeDependencies()
    {
        if (deliveryManager == null) deliveryManager = FindFirstObjectByType<DeliveryManager>();
        if (backupDataManager == null) backupDataManager = FindFirstObjectByType<BackupDataManager>();
        if (homePanelUI == null) homePanelUI = FindFirstObjectByType<HomePanelUI>();
        if (deliveryListPanelUI == null) deliveryListPanelUI = FindFirstObjectByType<DeliveryListPanelUI>();
        if (statsPanelUI == null) statsPanelUI = FindFirstObjectByType<StatsPanelUI>();
    }

    private void RegisterEvents()
    {
        RegisterButton(btnPrevYear, ShowPreviousYear);
        RegisterButton(btnNextYear, ShowNextYear);
        RegisterButton(btnRefresh, RefreshSelectedYear);
        RegisterButton(btnBackupToggle, ToggleBackupPanel);
        RegisterButton(btnBackupAllData, OnClickBackupAllData);
        RegisterButton(btnExportBackup, OnClickExportBackup);
        RegisterButton(btnImportBackup, OnClickImportBackup);
    }

    private void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void ShowPreviousYear()
    {
        selectedYear -= 1;
        RefreshYear(selectedYear);
    }

    private void ShowNextYear()
    {
        selectedYear += 1;
        RefreshYear(selectedYear);
    }

    private void RefreshSelectedYear()
    {
        TryInitializeDependencies();
        if (deliveryManager != null) deliveryManager.EnsureMissingDailyFixedExpenses(DateTime.Today);
        RefreshYear(selectedYear);
    }

    // Kept for existing linked panels that refresh SettlementPanelUI after record changes.
    public void RefreshSettlementUI()
    {
        RefreshSelectedYear();
    }
    public void RefreshYear(int year)
    {
        TryInitializeDependencies();
        selectedYear = year;

        if (txtYearTitle != null) txtYearTitle.text = $"{selectedYear}년 월별 정산";
        if (deliveryManager == null) return;

        for (int month = 1; month <= 12; month++)
        {
            if (monthlyCards != null && monthlyCards.Length >= month && monthlyCards[month - 1] != null)
            {
                monthlyCards[month - 1].Bind(selectedYear, month, deliveryManager.GetSummaryForMonth(selectedYear, month));
            }
        }
    }

    private void ToggleBackupPanel()
    {
        SetBackupPanelVisible(panelBackup == null || !panelBackup.activeSelf);
    }

    private void SetBackupPanelVisible(bool visible)
    {
        if (panelBackup != null) panelBackup.SetActive(visible);
        SetButtonLabel(btnBackupToggle, visible ? "백업 닫기" : "백업 보기");
    }

    private void SetButtonLabel(Button button, string value)
    {
        if (button == null) return;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = value;
    }
    /// <summary>
    /// 전체 데이터 백업 버튼 클릭
    /// </summary>
    private void OnClickBackupAllData()
    {
        if (backupDataManager == null)
        {
            Debug.LogWarning("[SettlementPanelUI] BackupDataManager 참조가 없습니다.");
            return;
        }

        bool isSuccess = backupDataManager.CreateFullBackup(
            out string backupFileName,
            out string backupFullPath,
            out string resultMessage);

        if (txtBackupInfo != null)
        {
            txtBackupInfo.text = isSuccess
                ? $"백업 완료: {backupFileName}"
                : $"백업 실패: {resultMessage}";
        }

        Debug.Log($"[SettlementPanelUI] {resultMessage} / {backupFullPath}");
    }

    /// <summary>
    /// 앱 내부 최신 백업 파일을 Download 폴더로 내보낸다.
    /// </summary>
    private void OnClickExportBackup()
    {
        if (backupDataManager == null)
        {
            Debug.LogWarning("[SettlementPanelUI] BackupDataManager 참조가 없습니다.");
            return;
        }

        bool isSuccess = backupDataManager.ExportLatestBackupToDownloads(
            out string exportedFileName,
            out string exportedFullPath,
            out string resultMessage);

        if (txtBackupInfo != null)
        {
            txtBackupInfo.text = isSuccess
                ? $"내보내기 완료: {exportedFileName}"
                : $"내보내기 실패: {resultMessage}";
        }

        Debug.Log($"[SettlementPanelUI] {resultMessage} / {exportedFullPath}");
    }

    /// <summary>
    /// 백업 파일 불러오기
    /// Android 실기기에서는 파일 선택창을 열어 사용자가 직접 backup_*.json 파일을 선택한다.
    /// </summary>
    private void OnClickImportBackup()
    {
        if (backupDataManager == null)
        {
            Debug.LogWarning("[SettlementPanelUI] BackupDataManager 참조가 없습니다.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    if (NativeFilePicker.IsFilePickerBusy())
    {
        return;
    }

    NativeFilePicker.PickFile(
        OnBackupFilePicked,
        new string[] { "application/json", "text/plain", "*/*" }
    );
#else
        bool isSuccess = backupDataManager.ImportLatestBackup(
            out string importedFileName,
            out string importedFullPath,
            out string resultMessage);

        ApplyImportResult(isSuccess, importedFileName, importedFullPath, resultMessage);
#endif
    }


    /// <summary>
    /// NativeFilePicker에서 선택된 파일 경로를 받아 백업 복원을 실행한다.
    /// </summary>
    private void OnBackupFilePicked(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            if (txtBackupInfo != null)
            {
                txtBackupInfo.text = "백업 파일 선택이 취소되었습니다";
            }

            return;
        }

        bool isSuccess = backupDataManager.ImportBackupFromFilePath(
            filePath,
            out string importedFileName,
            out string importedFullPath,
            out string resultMessage);

        ApplyImportResult(isSuccess, importedFileName, importedFullPath, resultMessage);
    }

    /// <summary>
    /// 백업 불러오기 결과 표시와 화면 갱신
    /// </summary>
    private void ApplyImportResult(bool isSuccess, string importedFileName, string importedFullPath, string resultMessage)
    {
        if (txtBackupInfo != null)
        {
            txtBackupInfo.text = isSuccess
                ? $"불러오기 완료: {importedFileName}"
                : $"불러오기 실패: {resultMessage}";
        }

        if (isSuccess)
        {
            RefreshSettlementUI();
            RefreshLinkedPanelsAfterImport();
        }

        Debug.Log($"[SettlementPanelUI] {resultMessage} / {importedFullPath}");
    }

    /// <summary>
    /// 백업 불러오기 후 주요 화면 갱신
    /// </summary>
    private void RefreshLinkedPanelsAfterImport()
    {
        if (homePanelUI != null)
        {
            homePanelUI.RefreshHomeSummary();
        }

        if (deliveryListPanelUI != null)
        {
            deliveryListPanelUI.RefreshList();
        }

        if (statsPanelUI != null)
        {
            statsPanelUI.ShowTodayStats();
        }
    }
}
