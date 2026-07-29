
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomePanelUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private DeliveryManager deliveryManager;
    [SerializeField] private WorkSessionManager workSessionManager;

    [Header("Header UI")]
    [SerializeField] private TMP_Text txtHeaderWorkStatus;
    [SerializeField] private TMP_Text txtHeaderTodayDate;

    [Header("Summary Texts")]
    [SerializeField] private TMP_Text txtCurrentStatus;
    [SerializeField] private TMP_Text txtTodayCount;
    [SerializeField] private TMP_Text txtTodayGross;
    [SerializeField] private TMP_Text txtTodayNet;
    [SerializeField] private TMP_Text txtMonthGross;
    [SerializeField] private TMP_Text txtPreviewEmpty;
    [SerializeField] private TMP_Text txtOverallNet;

    [Header("Work Buttons")]
    [SerializeField] private Button btnStartWork;
    [SerializeField] private Button btnEndWork;

    [Header("Manual Work Edit")]
    [SerializeField] private Button btnToggleWorkEdit;
    [SerializeField] private GameObject panelWorkDisplayRoot;
    [SerializeField] private GameObject workButtonRowObject;
    [SerializeField] private GameObject toggleWorkEditObject;
    [SerializeField] private GameObject panelWorkEditRoot;
    [SerializeField] private TMP_Text txtToggleWorkEditLabel;
    [SerializeField] private TMP_InputField inputWorkDate;
    [SerializeField] private TMP_InputField inputWorkStartTime;
    [SerializeField] private TMP_InputField inputWorkEndTime;
    [SerializeField] private Button btnSaveWorkTime;
    [SerializeField] private Button btnCloseWorkEdit;

    [Header("Quick Action Buttons")]
    [SerializeField] private Button btnNewDelivery;

    [Header("Panel References")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject deliveryInputPanel;

    private float nextHeaderRefreshTime = 0f;
    private bool isWorkEditOpen = false;

    private void Awake()
    {
        TryInitializeDependencies();
        RegisterEvents();
        InitializeUI();
    }

    private void OnEnable()
    {
        RefreshHomeSummary();
        RefreshHeaderRuntimeTexts();
        RefreshWorkEditInputs();
        nextHeaderRefreshTime = 0f;
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (Time.unscaledTime < nextHeaderRefreshTime)
        {
            return;
        }

        nextHeaderRefreshTime = Time.unscaledTime + 1f;
        RefreshHeaderRuntimeTexts();
    }

    private void TryInitializeDependencies()
    {
        if (deliveryManager == null)
        {
            deliveryManager = FindFirstObjectByType<DeliveryManager>();
        }

        if (workSessionManager == null)
        {
            workSessionManager = FindFirstObjectByType<WorkSessionManager>();
        }

        if (txtToggleWorkEditLabel == null && btnToggleWorkEdit != null)
        {
            txtToggleWorkEditLabel = btnToggleWorkEdit.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void RegisterEvents()
    {
        if (btnStartWork != null)
        {
            btnStartWork.onClick.RemoveAllListeners();
            btnStartWork.onClick.AddListener(OnClickStartWork);
        }

        if (btnEndWork != null)
        {
            btnEndWork.onClick.RemoveAllListeners();
            btnEndWork.onClick.AddListener(OnClickEndWork);
        }

        if (btnNewDelivery != null)
        {
            btnNewDelivery.onClick.RemoveAllListeners();
            btnNewDelivery.onClick.AddListener(OpenDeliveryInputPanel);
        }

        if (btnToggleWorkEdit != null)
        {
            btnToggleWorkEdit.onClick.RemoveAllListeners();
            btnToggleWorkEdit.onClick.AddListener(OnClickToggleWorkEdit);
        }

        if (btnCloseWorkEdit != null)
        {
            btnCloseWorkEdit.onClick.RemoveAllListeners();
            btnCloseWorkEdit.onClick.AddListener(OnClickCloseWorkEdit);
        }

        if (btnSaveWorkTime != null)
        {
            btnSaveWorkTime.onClick.RemoveAllListeners();
            btnSaveWorkTime.onClick.AddListener(OnClickSaveWorkTime);
        }

        if (inputWorkDate != null)
        {
            inputWorkDate.onEndEdit.RemoveAllListeners();
            inputWorkDate.onEndEdit.AddListener(OnEndEditWorkDate);
        }

        if (inputWorkStartTime != null)
        {
            inputWorkStartTime.onEndEdit.RemoveAllListeners();
            inputWorkStartTime.onEndEdit.AddListener(OnEndEditWorkStartTime);
        }

        if (inputWorkEndTime != null)
        {
            inputWorkEndTime.onEndEdit.RemoveAllListeners();
            inputWorkEndTime.onEndEdit.AddListener(OnEndEditWorkEndTime);
        }
    }

    private void OnClickCloseWorkEdit()
    {
        ApplyWorkEditVisibility(false);
    }


    private void InitializeUI()
    {
        RefreshHeaderRuntimeTexts();
        RefreshWorkEditInputs();
        ApplyWorkEditVisibility(false);
    }

    public void RefreshHomeSummary()
    {
        TryInitializeDependencies();

        if (deliveryManager == null)
        {
            Debug.LogWarning("[HomePanelUI] DeliveryManager 참조가 없습니다.");
            return;
        }

        DeliveryManager.DeliverySummary todaySummary = deliveryManager.GetTodaySummary();
        DeliveryManager.DeliverySummary monthSummary = deliveryManager.GetCurrentMonthSummary();
        DeliveryManager.DeliverySummary allSummary = deliveryManager.GetAllSummary();

        int todayWorkedMinutes = workSessionManager != null ? workSessionManager.GetTodayWorkedMinutes() : 0;

        RefreshHeaderRuntimeTexts();

        if (txtTodayCount != null)
        {
            txtTodayCount.text = $"오늘 총 건수: {todaySummary.TotalCount}건";
        }

        if (txtTodayGross != null)
        {
            txtTodayGross.text = $"오늘 총수입: {todaySummary.TotalGrossRevenue:N0}원";
        }

        if (txtTodayNet != null)
        {
            txtTodayNet.text = $"오늘 실수령액: {todaySummary.TotalNetRevenue:N0}원";
        }

        if (txtMonthGross != null)
        {
            txtMonthGross.text = $"이번 달 누적: {monthSummary.TotalGrossRevenue:N0}원";
        }

        if (txtOverallNet != null)
        {
            txtOverallNet.text = $"전체 누적: {allSummary.TotalNetRevenue:N0}원";
        }

        if (txtPreviewEmpty != null)
        {
            if (todayWorkedMinutes > 0 || todaySummary.TotalCount > 0)
            {
                string workedText = workSessionManager != null
                    ? workSessionManager.FormatMinutes(todayWorkedMinutes)
                    : "0분";

                txtPreviewEmpty.text = $"오늘 근무 {workedText} / 저장 기록 {todaySummary.TotalCount}건";
            }
            else
            {
                txtPreviewEmpty.text = "아직 저장된 기록이 없습니다";
            }
        }
    }

    private void RefreshHeaderRuntimeTexts()
    {
        if (txtHeaderTodayDate != null)
        {
            txtHeaderTodayDate.text = DateTime.Now.ToString("yyyy-MM-dd dddd HH:mm");
        }

        string headerLabel = "근무 전 / 0분";
        string detailLabel = "현재 상태: 근무 전 / 오늘 0분";

        if (workSessionManager != null)
        {
            headerLabel = workSessionManager.GetHeaderStatusText();
            detailLabel = workSessionManager.GetStatusText();
        }

        if (txtHeaderWorkStatus != null)
        {
            txtHeaderWorkStatus.text = headerLabel;
        }

        if (txtCurrentStatus != null)
        {
            txtCurrentStatus.text = detailLabel;
        }
    }

    private void RefreshWorkEditInputs()
    {
        if (inputWorkDate == null || inputWorkStartTime == null || inputWorkEndTime == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(inputWorkDate.text))
        {
            inputWorkDate.text = DateTime.Now.ToString("yyyy-MM-dd");
        }

        string dateText = inputWorkDate.text.Trim();

        bool isDateParsed = DateTime.TryParseExact(
            dateText,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime parsedDate);

        if (!isDateParsed || workSessionManager == null)
        {
            return;
        }

        WorkSession session = workSessionManager.GetSessionByDate(parsedDate);

        if (session == null)
        {
            if (string.IsNullOrWhiteSpace(inputWorkStartTime.text))
            {
                inputWorkStartTime.text = "09:00";
            }

            if (string.IsNullOrWhiteSpace(inputWorkEndTime.text))
            {
                inputWorkEndTime.text = "18:00";
            }

            return;
        }

        inputWorkStartTime.text = session.StartTime;
        inputWorkEndTime.text = session.EndTime;
    }

    private void OnClickStartWork()
    {
        if (workSessionManager != null)
        {
            workSessionManager.StartWork();
        }

        RefreshHomeSummary();
        RefreshWorkEditInputs();
    }

    private void OnClickEndWork()
    {
        if (workSessionManager != null)
        {
            workSessionManager.EndWork();
        }

        RefreshHomeSummary();
        RefreshWorkEditInputs();
    }

    private void OnClickToggleWorkEdit()
    {
        ApplyWorkEditVisibility(!isWorkEditOpen);
        RefreshWorkEditInputs();
    }

    private void ApplyWorkEditVisibility(bool isVisible)
    {
        isWorkEditOpen = isVisible;

        if (panelWorkDisplayRoot != null)
        {
            panelWorkDisplayRoot.SetActive(!isVisible);
        }

        if (workButtonRowObject != null)
        {
            workButtonRowObject.SetActive(!isVisible);
        }

        if (toggleWorkEditObject != null)
        {
            toggleWorkEditObject.SetActive(!isVisible);
        }

        if (panelWorkEditRoot != null)
        {
            panelWorkEditRoot.SetActive(isVisible);
        }

        if (txtToggleWorkEditLabel != null)
        {
            txtToggleWorkEditLabel.text = isVisible ? "근무시간 수정 닫기" : "근무시간 수정";
        }
    }

    private void OnClickSaveWorkTime()
    {
        if (workSessionManager == null || inputWorkDate == null || inputWorkStartTime == null || inputWorkEndTime == null)
        {
            return;
        }

        inputWorkDate.text = AutoFormatWorkDate(inputWorkDate.text);
        inputWorkStartTime.text = AutoFormatWorkTime(inputWorkStartTime.text);
        inputWorkEndTime.text = AutoFormatWorkTime(inputWorkEndTime.text);

        bool isSaved = workSessionManager.SaveWorkSessionByDate(
            inputWorkDate.text.Trim(),
            inputWorkStartTime.text.Trim(),
            inputWorkEndTime.text.Trim(),
            out string resultMessage);

        Debug.Log($"[HomePanelUI] {resultMessage}");

        if (isSaved)
        {
            RefreshHomeSummary();
            RefreshWorkEditInputs();
            ApplyWorkEditVisibility(false);
        }
    }

    private void OnEndEditWorkDate(string rawText)
    {
        if (inputWorkDate == null)
        {
            return;
        }

        inputWorkDate.text = AutoFormatWorkDate(rawText);
    }

    private void OnEndEditWorkStartTime(string rawText)
    {
        if (inputWorkStartTime == null)
        {
            return;
        }

        inputWorkStartTime.text = AutoFormatWorkTime(rawText);
    }

    private void OnEndEditWorkEndTime(string rawText)
    {
        if (inputWorkEndTime == null)
        {
            return;
        }

        inputWorkEndTime.text = AutoFormatWorkTime(rawText);
    }

    private string AutoFormatWorkDate(string rawText)
    {
        string digits = GetDigitsOnly(rawText);

        if (string.IsNullOrWhiteSpace(digits))
        {
            return string.Empty;
        }

        if (digits.Length == 3 || digits.Length == 4)
        {
            string mmdd = digits.PadLeft(4, '0');
            int currentYear = DateTime.Now.Year;

            if (TryBuildDateString(currentYear, mmdd.Substring(0, 2), mmdd.Substring(2, 2), out string formattedDate))
            {
                return formattedDate;
            }
        }

        if (digits.Length >= 8)
        {
            string yyyy = digits.Substring(0, 4);
            string mm = digits.Substring(4, 2);
            string dd = digits.Substring(6, 2);

            if (TryBuildDateString(yyyy, mm, dd, out string formattedDate))
            {
                return formattedDate;
            }
        }

        return rawText.Trim();
    }

    private string AutoFormatWorkTime(string rawText)
    {
        string digits = GetDigitsOnly(rawText);

        if (string.IsNullOrWhiteSpace(digits))
        {
            return string.Empty;
        }

        if (digits.Length <= 2)
        {
            if (int.TryParse(digits, out int hourOnly) && hourOnly >= 0 && hourOnly <= 23)
            {
                return $"{hourOnly:00}:00";
            }

            return rawText.Trim();
        }

        if (digits.Length == 3 || digits.Length == 4)
        {
            string hhmm = digits.PadLeft(4, '0');

            if (int.TryParse(hhmm.Substring(0, 2), out int hour) &&
                int.TryParse(hhmm.Substring(2, 2), out int minute) &&
                hour >= 0 && hour <= 23 &&
                minute >= 0 && minute <= 59)
            {
                return $"{hour:00}:{minute:00}";
            }
        }

        return rawText.Trim();
    }

    private string GetDigitsOnly(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        for (int i = 0; i < rawText.Length; i++)
        {
            if (char.IsDigit(rawText[i]))
            {
                builder.Append(rawText[i]);
            }
        }

        return builder.ToString();
    }

    private bool TryBuildDateString(int year, string monthText, string dayText, out string formattedDate)
    {
        formattedDate = string.Empty;

        if (!int.TryParse(monthText, out int month) || !int.TryParse(dayText, out int day))
        {
            return false;
        }

        if (month < 1 || month > 12)
        {
            return false;
        }

        int maxDay = DateTime.DaysInMonth(year, month);

        if (day < 1 || day > maxDay)
        {
            return false;
        }

        formattedDate = $"{year:0000}-{month:00}-{day:00}";
        return true;
    }

    private bool TryBuildDateString(string yearText, string monthText, string dayText, out string formattedDate)
    {
        formattedDate = string.Empty;

        if (!int.TryParse(yearText, out int year))
        {
            return false;
        }

        return TryBuildDateString(year, monthText, dayText, out formattedDate);
    }

    private void OpenDeliveryInputPanel()
    {
        if (homePanel != null)
        {
            homePanel.SetActive(false);
        }

        if (deliveryInputPanel != null)
        {
            deliveryInputPanel.SetActive(true);
        }
    }
}
