using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsPanelUI : MonoBehaviour
{
    private enum StatsPeriodType
    {
        Today = 0,
        Week = 1,
        Month = 2,
        Year = 3,
        All = 4,
        Custom = 5
    }

    private enum AnalysisItemType
    {
        GrossRevenue = 0,
        TotalExpense = 1,
        NetRevenue = 2,
        BaseFee = 3,
        BaseFeeVatSource = 4,
        VatAmount = 5,
        ExtraFee = 6,
        CommissionFee = 7,
        DataUsageFee = 8,
        OtherDeduction = 9,
        ElectricCharge = 10,
        TollFee = 11
    }

    [Header("Dependencies")]
    [SerializeField] private DeliveryManager deliveryManager;
    [SerializeField] private WorkSessionManager workSessionManager;

    [Header("Date / Amount Filter UI")]
    [SerializeField] private TMP_InputField inputStartDate;
    [SerializeField] private TMP_InputField inputEndDate;
    [SerializeField] private TMP_InputField inputMinAmount;
    [SerializeField] private TMP_InputField inputMaxAmount;
    [SerializeField] private Button btnApplyFilters;
    [SerializeField] private TMP_Text txtFilterGuide;

    [Header("Summary UI")]
    [SerializeField] private TMP_Text txtSelectedPeriod;
    [SerializeField] private TMP_Text txtCurrentCount;
    [SerializeField] private TMP_Text txtCurrentGross;
    [SerializeField] private TMP_Text txtCurrentExpense;
    [SerializeField] private TMP_Text txtCurrentNet;
    [SerializeField] private TMP_Text txtTodayWorkTime;
    [SerializeField] private TMP_Text txtMonthWorkTime;

    [Header("Analysis UI")]
    [SerializeField] private TMP_Dropdown dropdownAnalysisItem;
    [SerializeField] private TMP_Text txtAnalysisValue;
    [SerializeField] private TMP_Text txtNote;

    [Header("Buttons")]
    [SerializeField] private Button btnStatsToday;
    [SerializeField] private Button btnStatsWeek;
    [SerializeField] private Button btnStatsMonth;
    [SerializeField] private Button btnStatsYear;
    [SerializeField] private Button btnStatsAll;

    private StatsPeriodType currentPeriod = StatsPeriodType.Today;
    private DeliveryManager.DeliverySummary currentSummary = new DeliveryManager.DeliverySummary();
    private DateTime currentCustomStartDate;
    private DateTime currentCustomEndDate;

    private void Awake()
    {
        TryInitializeDependencies();
        RegisterEvents();
        InitializeDropdown();
        InitializeUI();
    }

    private void OnEnable()
    {
        ApplyCurrentFilter();
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
    }

    private void RegisterEvents()
    {
        if (btnStatsToday != null)
        {
            btnStatsToday.onClick.AddListener(ShowTodayStats);
        }

        if (btnStatsWeek != null)
        {
            btnStatsWeek.onClick.AddListener(ShowWeekStats);
        }

        if (btnStatsMonth != null)
        {
            btnStatsMonth.onClick.AddListener(ShowMonthStats);
        }

        if (btnStatsYear != null)
        {
            btnStatsYear.onClick.AddListener(ShowYearStats);
        }

        if (btnStatsAll != null)
        {
            btnStatsAll.onClick.AddListener(ShowAllStats);
        }

        if (btnApplyFilters != null)
        {
            btnApplyFilters.onClick.AddListener(OnClickApplyFilters);
        }

        if (dropdownAnalysisItem != null)
        {
            dropdownAnalysisItem.onValueChanged.AddListener(delegate { OnAnalysisItemChanged(); });
        }

        RegisterDateAutoFormat(inputStartDate, true);
        RegisterDateAutoFormat(inputEndDate, false);
    }

    private void RegisterDateAutoFormat(TMP_InputField inputField, bool useMonthStartForShortInput)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.onEndEdit.AddListener(delegate
        {
            string formatted = AutoFormatFilterDate(inputField.text, useMonthStartForShortInput);
            inputField.SetTextWithoutNotify(formatted);
        });
    }

    private string AutoFormatFilterDate(string rawText, bool useMonthStartForShortInput)
    {
        string digits = GetDigitsOnly(rawText);

        if (string.IsNullOrWhiteSpace(digits))
        {
            return string.Empty;
        }

        int currentYear = DateTime.Now.Year;

        if (digits.Length == 1 || digits.Length == 2)
        {
            int month = Mathf.Clamp(int.Parse(digits), 1, 12);
            int day = useMonthStartForShortInput ? 1 : DateTime.DaysInMonth(currentYear, month);
            return $"{currentYear:0000}-{month:00}-{day:00}";
        }

        if (digits.Length == 3 || digits.Length == 4)
        {
            string mmdd = digits.PadLeft(4, '0');
            string mm = mmdd.Substring(0, 2);
            string dd = mmdd.Substring(2, 2);

            if (TryBuildFilterDateString(currentYear, mm, dd, out string formattedDate))
            {
                return formattedDate;
            }
        }

        if (digits.Length >= 8)
        {
            string yyyy = digits.Substring(0, 4);
            string mm = digits.Substring(4, 2);
            string dd = digits.Substring(6, 2);

            if (TryBuildFilterDateString(yyyy, mm, dd, out string formattedDate))
            {
                return formattedDate;
            }
        }

        return rawText.Trim();
    }

    private void InitializeDropdown()
    {
        if (dropdownAnalysisItem == null)
        {
            return;
        }

        dropdownAnalysisItem.ClearOptions();
        dropdownAnalysisItem.AddOptions(new List<string>
        {
            "총수입",
            "총비용",
            "실수령액",
            "기본요금",
            "기본요금 + 부가세",
            "부가세",
            "추가요금",
            "회사입금",
            "데이터 사용료",
            "차감액",
            "차량전기비",
            "통행료"
        });
        dropdownAnalysisItem.value = 0;
        dropdownAnalysisItem.RefreshShownValue();
    }

    private void InitializeUI()
    {
        if (inputStartDate != null && string.IsNullOrWhiteSpace(inputStartDate.text))
        {
            inputStartDate.text = DateTime.Now.ToString("yyyy-MM-01");
        }

        if (inputEndDate != null && string.IsNullOrWhiteSpace(inputEndDate.text))
        {
            inputEndDate.text = DateTime.Now.ToString("yyyy-MM-dd");
        }

        if (txtFilterGuide != null)
        {
            txtFilterGuide.text = "날짜는 yyyy-MM-dd 형식, 금액은 숫자만 입력";
        }

        if (txtSelectedPeriod != null)
        {
            txtSelectedPeriod.text = "현재 보기: 오늘";
        }

        if (txtCurrentCount != null)
        {
            txtCurrentCount.text = "총 건수: 0건";
        }

        if (txtCurrentGross != null)
        {
            txtCurrentGross.text = "총수입: 0원";
        }

        if (txtCurrentExpense != null)
        {
            txtCurrentExpense.text = "총비용: 0원";
        }

        if (txtCurrentNet != null)
        {
            txtCurrentNet.text = "실수령액: 0원";
        }

        if (txtTodayWorkTime != null)
        {
            txtTodayWorkTime.text = "오늘 근무시간: 0분";
        }

        if (txtMonthWorkTime != null)
        {
            txtMonthWorkTime.text = "이번 달 누적 근무시간: 0분";
        }

        if (txtAnalysisValue != null)
        {
            txtAnalysisValue.text = "0원";
        }

        if (txtNote != null)
        {
            txtNote.text = "선택한 기간 기준으로 계산됩니다.";
        }
    }

    public void ShowTodayStats() { currentPeriod = StatsPeriodType.Today; ApplyCurrentFilter(); }
    public void ShowWeekStats() { currentPeriod = StatsPeriodType.Week; ApplyCurrentFilter(); }
    public void ShowMonthStats() { currentPeriod = StatsPeriodType.Month; ApplyCurrentFilter(); }
    public void ShowYearStats() { currentPeriod = StatsPeriodType.Year; ApplyCurrentFilter(); }
    public void ShowAllStats() { currentPeriod = StatsPeriodType.All; ApplyCurrentFilter(); }

    private void OnClickApplyFilters()
    {
        if (inputStartDate != null)
        {
            inputStartDate.SetTextWithoutNotify(AutoFormatFilterDate(inputStartDate.text, true));
        }

        if (inputEndDate != null)
        {
            inputEndDate.SetTextWithoutNotify(AutoFormatFilterDate(inputEndDate.text, false));
        }

        if (!TryParseDateRangeInputs(out DateTime startDate, out DateTime endDate))
        {
            if (txtFilterGuide != null)
            {
                txtFilterGuide.text = "날짜 형식을 yyyy-MM-dd 로 입력해 주세요";
            }
            return;
        }

        currentCustomStartDate = startDate.Date;
        currentCustomEndDate = endDate.Date;
        currentPeriod = StatsPeriodType.Custom;

        ApplyCurrentFilter();
    }

    private void OnAnalysisItemChanged()
    {
        if (HasAmountFilter())
        {
            ApplyCurrentFilter();
        }
        else
        {
            RefreshAnalysisValue();
        }
    }

    private void ApplyCurrentFilter()
    {
        TryInitializeDependencies();

        if (deliveryManager == null)
        {
            Debug.LogWarning("[StatsPanelUI] DeliveryManager 참조가 없습니다.");
            return;
        }

        List<DeliveryRecord> baseRecords = GetRecordsForCurrentPeriod();
        List<DeliveryRecord> filteredRecords = ApplyAmountFilter(baseRecords);

        currentSummary = deliveryManager.GetSummaryByRecords(filteredRecords);

        UpdateSummaryTexts();
        UpdateWorkTimeTexts();
        RefreshAnalysisValue();
        UpdateFilterGuide(filteredRecords.Count);
    }

    private void UpdateWorkTimeTexts()
    {
        int todayWorkedMinutes = workSessionManager != null ? workSessionManager.GetTodayWorkedMinutes() : 0;
        int monthWorkedMinutes = workSessionManager != null ? workSessionManager.GetCurrentMonthWorkedMinutes() : 0;

        if (txtTodayWorkTime != null)
        {
            txtTodayWorkTime.text = $"오늘 근무시간: {FormatMinutes(todayWorkedMinutes)}";
        }

        if (txtMonthWorkTime != null)
        {
            txtMonthWorkTime.text = $"이번 달 누적 근무시간: {FormatMinutes(monthWorkedMinutes)}";
        }
    }

    private string FormatMinutes(int totalMinutes)
    {
        int safeMinutes = Mathf.Max(0, totalMinutes);
        int hour = safeMinutes / 60;
        int minute = safeMinutes % 60;

        if (hour <= 0)
        {
            return $"{minute}분";
        }

        return $"{hour}시간 {minute}분";
    }

    private List<DeliveryRecord> GetRecordsForCurrentPeriod()
    {
        if (deliveryManager == null)
        {
            return new List<DeliveryRecord>();
        }

        switch (currentPeriod)
        {
            case StatsPeriodType.Today: return deliveryManager.GetTodayRecords();
            case StatsPeriodType.Week: return deliveryManager.GetCurrentWeekRecords();
            case StatsPeriodType.Month: return deliveryManager.GetCurrentMonthRecords();
            case StatsPeriodType.Year: return deliveryManager.GetCurrentYearRecords();
            case StatsPeriodType.All: return deliveryManager.GetAllRecords();
            case StatsPeriodType.Custom: return deliveryManager.GetRecordsByDateRange(currentCustomStartDate, currentCustomEndDate);
        }

        return new List<DeliveryRecord>();
    }

    private List<DeliveryRecord> ApplyAmountFilter(List<DeliveryRecord> sourceRecords)
    {
        List<DeliveryRecord> result = new List<DeliveryRecord>();

        if (sourceRecords == null)
        {
            return result;
        }

        bool hasMin = TryParseAmountInput(inputMinAmount, out int minAmount);
        bool hasMax = TryParseAmountInput(inputMaxAmount, out int maxAmount);

        if (!hasMin && !hasMax)
        {
            return new List<DeliveryRecord>(sourceRecords);
        }

        if (hasMin && hasMax && minAmount > maxAmount)
        {
            int temp = minAmount;
            minAmount = maxAmount;
            maxAmount = temp;
        }

        AnalysisItemType selectedItem = GetSelectedAnalysisItem();

        for (int i = 0; i < sourceRecords.Count; i++)
        {
            DeliveryRecord record = sourceRecords[i];
            if (record == null) { continue; }

            int targetValue = GetRecordAmountByItem(record, selectedItem);
            bool passMin = !hasMin || targetValue >= minAmount;
            bool passMax = !hasMax || targetValue <= maxAmount;

            if (passMin && passMax)
            {
                result.Add(record);
            }
        }

        return result;
    }

    private void UpdateSummaryTexts()
    {
        if (txtSelectedPeriod != null) txtSelectedPeriod.text = $"현재 보기: {GetPeriodLabel(currentPeriod)}";
        if (txtCurrentCount != null) txtCurrentCount.text = $"총 건수: {currentSummary.TotalCount}건";
        if (txtCurrentGross != null) txtCurrentGross.text = $"총수입: {currentSummary.TotalGrossRevenue:N0}원";
        if (txtCurrentExpense != null) txtCurrentExpense.text = $"총비용: {currentSummary.TotalExpense:N0}원";
        if (txtCurrentNet != null) txtCurrentNet.text = $"실수령액: {currentSummary.TotalNetRevenue:N0}원";
    }

    private void RefreshAnalysisValue()
    {
        AnalysisItemType selectedItem = GetSelectedAnalysisItem();
        int value = GetAnalysisValue(currentSummary, selectedItem);

        if (txtAnalysisValue != null)
        {
            txtAnalysisValue.text = $"{GetAnalysisLabel(selectedItem)} 합계: {value:N0}원";
        }

        if (txtNote != null)
        {
            txtNote.text = $"{GetPeriodLabel(currentPeriod)} 기준 {GetAnalysisLabel(selectedItem)} 합계입니다.";
        }
    }

    private void UpdateFilterGuide(int filteredCount)
    {
        string guide = $"현재 {GetPeriodLabel(currentPeriod)} / {filteredCount}건 표시 중";

        bool hasMin = TryParseAmountInput(inputMinAmount, out int minAmount);
        bool hasMax = TryParseAmountInput(inputMaxAmount, out int maxAmount);

        if (hasMin || hasMax)
        {
            string rangeText = string.Empty;

            if (hasMin && hasMax) rangeText = $"{minAmount:N0}원 ~ {maxAmount:N0}원";
            else if (hasMin) rangeText = $"{minAmount:N0}원 이상";
            else rangeText = $"{maxAmount:N0}원 이하";

            guide += $" / {GetAnalysisLabel(GetSelectedAnalysisItem())} {rangeText}";
        }

        if (txtFilterGuide != null)
        {
            txtFilterGuide.text = guide;
        }
    }

    private bool TryParseDateRangeInputs(out DateTime startDate, out DateTime endDate)
    {
        startDate = DateTime.MinValue;
        endDate = DateTime.MinValue;

        string startText = inputStartDate != null ? inputStartDate.text.Trim() : string.Empty;
        string endText = inputEndDate != null ? inputEndDate.text.Trim() : string.Empty;

        bool isStartParsed = DateTime.TryParseExact(startText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate);
        bool isEndParsed = DateTime.TryParseExact(endText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate);

        if (!isStartParsed || !isEndParsed)
        {
            return false;
        }

        if (startDate.Date > endDate.Date)
        {
            DateTime temp = startDate;
            startDate = endDate;
            endDate = temp;
        }

        return true;
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

    private bool TryBuildFilterDateString(int year, string monthText, string dayText, out string formattedDate)
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

    private bool TryBuildFilterDateString(string yearText, string monthText, string dayText, out string formattedDate)
    {
        formattedDate = string.Empty;

        if (!int.TryParse(yearText, out int year))
        {
            return false;
        }

        return TryBuildFilterDateString(year, monthText, dayText, out formattedDate);
    }

    private bool TryParseAmountInput(TMP_InputField inputField, out int value)
    {
        value = 0;

        if (inputField == null)
        {
            return false;
        }

        string raw = inputField.text.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return false;

        raw = raw.Replace(",", string.Empty);
        return int.TryParse(raw, out value);
    }

    private bool HasAmountFilter()
    {
        return TryParseAmountInput(inputMinAmount, out _) || TryParseAmountInput(inputMaxAmount, out _);
    }

    private AnalysisItemType GetSelectedAnalysisItem()
    {
        if (dropdownAnalysisItem == null) return AnalysisItemType.GrossRevenue;
        return (AnalysisItemType)dropdownAnalysisItem.value;
    }

    private int GetRecordAmountByItem(DeliveryRecord record, AnalysisItemType itemType)
    {
        if (record == null) return 0;

        switch (itemType)
        {
            case AnalysisItemType.GrossRevenue: return record.GrossRevenue;
            case AnalysisItemType.TotalExpense: return record.TotalExpense;
            case AnalysisItemType.NetRevenue: return record.NetRevenue;
            case AnalysisItemType.BaseFee: return record.BaseFee;
            case AnalysisItemType.BaseFeeVatSource: return record.BaseFeeVatSource;
            case AnalysisItemType.VatAmount: return record.VatAmount;
            case AnalysisItemType.ExtraFee: return record.ExtraFee;
            case AnalysisItemType.CommissionFee: return record.CommissionFee;
            case AnalysisItemType.DataUsageFee: return record.DataUsageFee;
            case AnalysisItemType.OtherDeduction: return record.OtherDeduction;
            case AnalysisItemType.ElectricCharge: return record.ElectricCharge;
            case AnalysisItemType.TollFee: return record.TollFee;
        }

        return 0;
    }

    private int GetAnalysisValue(DeliveryManager.DeliverySummary summary, AnalysisItemType itemType)
    {
        if (summary == null) return 0;

        switch (itemType)
        {
            case AnalysisItemType.GrossRevenue: return summary.TotalGrossRevenue;
            case AnalysisItemType.TotalExpense: return summary.TotalExpense;
            case AnalysisItemType.NetRevenue: return summary.TotalNetRevenue;
            case AnalysisItemType.BaseFee: return summary.TotalBaseFee;
            case AnalysisItemType.BaseFeeVatSource: return summary.TotalBaseFeeVatSource;
            case AnalysisItemType.VatAmount: return summary.TotalVatAmount;
            case AnalysisItemType.ExtraFee: return summary.TotalExtraFee;
            case AnalysisItemType.CommissionFee: return summary.TotalCommissionFee;
            case AnalysisItemType.DataUsageFee: return summary.TotalDataUsageFee;
            case AnalysisItemType.OtherDeduction: return summary.TotalOtherDeduction;
            case AnalysisItemType.ElectricCharge: return summary.TotalElectricCharge;
            case AnalysisItemType.TollFee: return summary.TotalTollFee;
        }

        return 0;
    }

    private string GetAnalysisLabel(AnalysisItemType itemType)
    {
        switch (itemType)
        {
            case AnalysisItemType.GrossRevenue: return "총수입";
            case AnalysisItemType.TotalExpense: return "총비용";
            case AnalysisItemType.NetRevenue: return "실수령액";
            case AnalysisItemType.BaseFee: return "기본요금";
            case AnalysisItemType.BaseFeeVatSource: return "기본요금 + 부가세";
            case AnalysisItemType.VatAmount: return "부가세";
            case AnalysisItemType.ExtraFee: return "추가요금";
            case AnalysisItemType.CommissionFee: return "회사입금";
            case AnalysisItemType.DataUsageFee: return "데이터 사용료";
            case AnalysisItemType.OtherDeduction: return "차감액";
            case AnalysisItemType.ElectricCharge: return "차량전기비";
            case AnalysisItemType.TollFee: return "통행료";
        }

        return "항목";
    }

    private string GetPeriodLabel(StatsPeriodType periodType)
    {
        switch (periodType)
        {
            case StatsPeriodType.Today: return "오늘";
            case StatsPeriodType.Week: return "최근 7일";
            case StatsPeriodType.Month: return "이번 달";
            case StatsPeriodType.Year: return "올해";
            case StatsPeriodType.All: return "전체";
            case StatsPeriodType.Custom: return $"{currentCustomStartDate:yyyy-MM-dd} ~ {currentCustomEndDate:yyyy-MM-dd}";
        }

        return "오늘";
    }
}
