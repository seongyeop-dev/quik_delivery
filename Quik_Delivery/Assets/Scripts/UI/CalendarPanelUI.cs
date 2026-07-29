using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarPanelUI : MonoBehaviour
{
    [Serializable]
    private class WeeklyChartData
    {
        public string Label;
        public int Value;
        public int WeekIndex;
    }

    private class CalendarDaySummary
    {
        public int WorkedMinutes = 0;
        public int DeliveryCount = 0;
        public int GrossRevenue = 0;
        public int TotalExpense = 0;
        public int NetRevenue = 0;
        public int VatAmount = 0;
    }

    [Header("Dependencies")]
    [SerializeField] private DeliveryManager deliveryManager;
    [SerializeField] private WorkSessionManager workSessionManager;

    [Header("Linked UI")]
    [SerializeField] private DeliveryListPanelUI deliveryListPanelUI;

    [Header("Panel References")]
    [SerializeField] private GameObject calendarPanel;
    [SerializeField] private GameObject deliveryListPanel;

    [Header("Optional Font")]
    [SerializeField] private TMP_FontAsset calendarFontAsset;

    [Header("Header UI")]
    [SerializeField] private TMP_Text txtAnalysisTitle;
    [SerializeField] private Button btnPrevMonth;
    [SerializeField] private Button btnNextMonth;
    [SerializeField] private TMP_Text txtCurrentMonth;

    [Header("Calendar Root")]
    [SerializeField] private Transform calendarGridRoot;

    [Header("Selected Day Summary")]
    [SerializeField] private TMP_Text txtSelectedDayCount;
    [SerializeField] private TMP_Text txtSelectedDayAmount;
    [SerializeField] private TMP_Text txtSelectedDayNote;

    [Header("Chart UI")]
    [SerializeField] private RectTransform chartContainer;
    [SerializeField] private TMP_Text txtChartGuide;

    [Header("Colors")]
    [SerializeField] private Color currentMonthCellColor = new Color(1f, 1f, 1f, 0.92f);
    [SerializeField] private Color selectedDayCellColor = new Color(0.74f, 0.87f, 1f, 1f);
    [SerializeField] private Color emptyCellColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color todayCellColor = new Color(0.92f, 0.97f, 0.92f, 1f);
    [SerializeField] private Color chartBarColor = new Color(0.55f, 0.72f, 0.95f, 1f);
    [SerializeField] private Color chartHighlightColor = new Color(0.23f, 0.49f, 0.90f, 1f);
    [SerializeField] private Color chartZeroBarColor = new Color(0.82f, 0.82f, 0.82f, 1f);

    private DateTime displayedMonth = DateTime.MinValue;
    private DateTime? selectedDate = null;

    private int selectedAnalysisItemIndex = 0;
    private string selectedAnalysisLabel = "총수입";

    private void Awake()
    {
        TryInitializeDependencies();
        RegisterEvents();
        InitializeState();
    }

    private void OnEnable()
    {
        if (displayedMonth == DateTime.MinValue)
        {
            displayedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        }

        RefreshCalendarView();
    }

    public void OpenFromStats(int analysisItemIndex, string analysisLabel)
    {
        selectedAnalysisItemIndex = Mathf.Clamp(analysisItemIndex, 0, 11);
        selectedAnalysisLabel = string.IsNullOrWhiteSpace(analysisLabel) ? "총수입" : analysisLabel;

        displayedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        selectedDate = null;

        RefreshCalendarView();
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
        if (btnPrevMonth != null)
        {
            btnPrevMonth.onClick.AddListener(OnClickPrevMonth);
        }

        if (btnNextMonth != null)
        {
            btnNextMonth.onClick.AddListener(OnClickNextMonth);
        }
    }

    private void InitializeState()
    {
        displayedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        if (txtSelectedDayCount != null)
        {
            txtSelectedDayCount.text = "건수: 0건 / 누적건수: 0건";
        }

        if (txtSelectedDayAmount != null)
        {
            txtSelectedDayAmount.text = "합계: 0원 / 누적액: 0원";
        }

        if (txtSelectedDayNote != null)
        {
            txtSelectedDayNote.text = "근무시간: 0:00 / 누적근무시간: 0:00";
        }

        if (txtChartGuide != null)
        {
            txtChartGuide.text = "주차별 흐름을 표시합니다";
        }
    }

    private void RefreshCalendarView()
    {
        UpdateHeaderTexts();
        BuildCalendarGrid();
        RefreshSelectedDaySummary();
        BuildWeeklyBarChart();
        RefreshChartGuide();
    }

    private void UpdateHeaderTexts()
    {
        if (txtAnalysisTitle != null)
        {
            txtAnalysisTitle.text = "월간 달력";
        }

        if (txtCurrentMonth != null)
        {
            txtCurrentMonth.text = $"{displayedMonth:yyyy년 M월}";
        }
    }

    private void BuildCalendarGrid()
    {
        if (calendarGridRoot == null)
        {
            Debug.LogWarning("[CalendarPanelUI] CalendarGridRoot 참조가 없습니다.");
            return;
        }

        ClearChildren(calendarGridRoot);

        DateTime firstDay = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month);
        int firstDayOfWeek = (int)firstDay.DayOfWeek;

        Dictionary<int, CalendarDaySummary> daySummaryMap = BuildDaySummaryMap(firstDay, daysInMonth);

        for (int cellIndex = 0; cellIndex < 42; cellIndex++)
        {
            int dayNumber = cellIndex - firstDayOfWeek + 1;

            if (dayNumber < 1 || dayNumber > daysInMonth)
            {
                CreateEmptyDayCell(cellIndex);
                continue;
            }

            DateTime cellDate = new DateTime(displayedMonth.Year, displayedMonth.Month, dayNumber);
            CalendarDaySummary summary = daySummaryMap.ContainsKey(dayNumber)
                ? daySummaryMap[dayNumber]
                : new CalendarDaySummary();

            CreateActiveDayCell(cellIndex, cellDate, summary);
        }
    }

    private Dictionary<int, CalendarDaySummary> BuildDaySummaryMap(DateTime firstDay, int daysInMonth)
    {
        Dictionary<int, CalendarDaySummary> daySummaryMap = new Dictionary<int, CalendarDaySummary>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            daySummaryMap[day] = new CalendarDaySummary();

            if (workSessionManager != null)
            {
                DateTime targetDate = new DateTime(displayedMonth.Year, displayedMonth.Month, day);
                daySummaryMap[day].WorkedMinutes = workSessionManager.GetWorkedMinutesByDate(targetDate);
            }
        }

        if (deliveryManager == null)
        {
            return daySummaryMap;
        }

        DateTime lastDay = new DateTime(displayedMonth.Year, displayedMonth.Month, daysInMonth);
        List<DeliveryRecord> monthRecords = deliveryManager.GetRecordsByDateRange(firstDay, lastDay);

        for (int i = 0; i < monthRecords.Count; i++)
        {
            DeliveryRecord record = monthRecords[i];

            if (record == null || string.IsNullOrWhiteSpace(record.Date))
            {
                continue;
            }

            if (!DateTime.TryParse(record.Date, out DateTime parsedDate))
            {
                continue;
            }

            int day = parsedDate.Day;

            if (!daySummaryMap.ContainsKey(day))
            {
                daySummaryMap[day] = new CalendarDaySummary();
            }

            CalendarDaySummary summary = daySummaryMap[day];

            if (IsCountableDeliveryRecord(record))
            {
                summary.DeliveryCount += 1;
            }

            summary.GrossRevenue += Mathf.Max(0, record.GrossRevenue);
            summary.TotalExpense += Mathf.Max(0, record.TotalExpense);
            summary.NetRevenue += record.NetRevenue;
            summary.VatAmount += Mathf.Max(0, record.VatAmount);
        }

        return daySummaryMap;
    }

    private Dictionary<int, int> BuildDayCountMap(DateTime firstDay, int daysInMonth)
    {
        Dictionary<int, int> dayCountMap = new Dictionary<int, int>();

        if (deliveryManager == null)
        {
            return dayCountMap;
        }

        DateTime lastDay = new DateTime(displayedMonth.Year, displayedMonth.Month, daysInMonth);
        List<DeliveryRecord> monthRecords = deliveryManager.GetRecordsByDateRange(firstDay, lastDay);

        for (int i = 0; i < monthRecords.Count; i++)
        {
            DeliveryRecord record = monthRecords[i];

            if (record == null || string.IsNullOrWhiteSpace(record.Date))
            {
                continue;
            }

            if (!DateTime.TryParse(record.Date, out DateTime parsedDate))
            {
                continue;
            }

            bool isDeliveryCount = IsCountableDeliveryRecord(record);

            if (!isDeliveryCount)
            {
                continue;
            }

            int day = parsedDate.Day;

            if (!dayCountMap.ContainsKey(day))
            {
                dayCountMap.Add(day, 0);
            }

            dayCountMap[day] += 1;
        }

        return dayCountMap;
    }

    private Dictionary<int, int> BuildDayWorkMinuteMap(int daysInMonth)
    {
        Dictionary<int, int> dayWorkMinuteMap = new Dictionary<int, int>();

        if (workSessionManager == null)
        {
            return dayWorkMinuteMap;
        }

        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime targetDate = new DateTime(displayedMonth.Year, displayedMonth.Month, day);
            int workedMinutes = workSessionManager.GetWorkedMinutesByDate(targetDate);

            if (workedMinutes > 0)
            {
                dayWorkMinuteMap[day] = workedMinutes;
            }
        }

        return dayWorkMinuteMap;
    }

    private void CreateEmptyDayCell(int index)
    {
        GameObject cellObject = new GameObject($"EmptyCell_{index}", typeof(RectTransform));
        cellObject.transform.SetParent(calendarGridRoot, false);

        Image image = cellObject.AddComponent<Image>();
        image.color = emptyCellColor;
        image.raycastTarget = false;
    }

    private void CreateActiveDayCell(int index, DateTime date, CalendarDaySummary summary)
    {
        GameObject cellObject = new GameObject($"DayCell_{index}_{date:dd}", typeof(RectTransform));
        cellObject.transform.SetParent(calendarGridRoot, false);

        Image image = cellObject.AddComponent<Image>();
        image.color = GetDayCellColor(date);

        Button button = cellObject.AddComponent<Button>();
        button.targetGraphic = image;

        DateTime capturedDate = date;
        button.onClick.AddListener(delegate { OnClickDayCell(capturedDate); });

        CreateDayNumberText(cellObject.transform, date.Day.ToString());
        CreateDayBodyText(cellObject.transform, summary);
    }

    private void CreateDayNumberText(Transform parent, string dayText)
    {
        GameObject textObject = new GameObject("Txt_DayNumber", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.72f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(3f, -3f);
        rect.offsetMax = new Vector2(-3f, -3f);

        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.font = GetCalendarFontAsset(tmp);
        tmp.text = dayText;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
    }

    private void CreateDayBodyText(Transform parent, CalendarDaySummary summary)
    {
        if (summary == null)
        {
            summary = new CalendarDaySummary();
        }

        GameObject textObject = new GameObject("Txt_DayBody", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0.82f);
        rect.offsetMin = new Vector2(2f, 2f);
        rect.offsetMax = new Vector2(-2f, -2f);

        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.font = GetCalendarFontAsset(tmp);
        tmp.text =
            $"{FormatCalendarWorkedTime(summary.WorkedMinutes)} / {summary.DeliveryCount}건\n" +
            $"수 {summary.GrossRevenue:N0}\n" +
            $"지 {summary.TotalExpense:N0}\n" +
            $"부 {summary.VatAmount:N0}\n" +
            $"실 {summary.NetRevenue:N0}";

        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.lineSpacing = -2f;
    }

    private void RefreshSelectedDaySummary()
    {
        DateTime firstDay = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month);
        Dictionary<int, CalendarDaySummary> daySummaryMap = BuildDaySummaryMap(firstDay, daysInMonth);
        CalendarDaySummary monthSummary = BuildMonthSummary(daySummaryMap);

        if (!selectedDate.HasValue)
        {
            if (txtSelectedDayCount != null)
            {
                txtSelectedDayCount.text = $"건수: 0건 / 누적건수: {monthSummary.DeliveryCount}건";
            }

            if (txtSelectedDayAmount != null)
            {
                txtSelectedDayAmount.text =
                    $"이번 달 수입: {monthSummary.GrossRevenue:N0}원\n" +
                    $"이번 달 지출: {monthSummary.TotalExpense:N0}원\n" +
                    $"이번 달 실수령: {monthSummary.NetRevenue:N0}원";
            }

            if (txtSelectedDayNote != null)
            {
                txtSelectedDayNote.text =
                    $"이번 달 부가세: {monthSummary.VatAmount:N0}원\n" +
                    $"누적근무시간: {FormatCalendarWorkedTime(monthSummary.WorkedMinutes)}";
            }

            return;
        }

        DateTime targetDate = selectedDate.Value.Date;
        CalendarDaySummary daySummary = daySummaryMap.ContainsKey(targetDate.Day)
            ? daySummaryMap[targetDate.Day]
            : new CalendarDaySummary();

        if (txtSelectedDayCount != null)
        {
            txtSelectedDayCount.text = $"건수: {daySummary.DeliveryCount}건 / 누적건수: {monthSummary.DeliveryCount}건";
        }

        if (txtSelectedDayAmount != null)
        {
            txtSelectedDayAmount.text =
                $"수입: {daySummary.GrossRevenue:N0}원 / 지출: {daySummary.TotalExpense:N0}원\n" +
                $"실수령: {daySummary.NetRevenue:N0}원";
        }

        if (txtSelectedDayNote != null)
        {
            txtSelectedDayNote.text =
                $"누적 수입: {monthSummary.GrossRevenue:N0}원 / 누적 지출: {monthSummary.TotalExpense:N0}원\n" +
                $"누적 실수령: {monthSummary.NetRevenue:N0}원 / 누적 부가세: {monthSummary.VatAmount:N0}원\n" +
                $"근무: {FormatCalendarWorkedTime(daySummary.WorkedMinutes)} / 누적근무: {FormatCalendarWorkedTime(monthSummary.WorkedMinutes)}";
        }
    }

    private List<WeeklyChartData> BuildWeeklyChartData()
    {
        List<WeeklyChartData> weeklyData = new List<WeeklyChartData>();

        DateTime firstDay = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month);
        int firstDayOfWeek = (int)firstDay.DayOfWeek;
        int weekCount = Mathf.CeilToInt((firstDayOfWeek + daysInMonth) / 7f);

        for (int i = 0; i < weekCount; i++)
        {
            weeklyData.Add(new WeeklyChartData
            {
                Label = $"{i + 1}주",
                Value = 0,
                WeekIndex = i
            });
        }

        if (deliveryManager == null)
        {
            return weeklyData;
        }

        DateTime lastDay = new DateTime(displayedMonth.Year, displayedMonth.Month, daysInMonth);
        List<DeliveryRecord> monthRecords = deliveryManager.GetRecordsByDateRange(firstDay, lastDay);

        for (int i = 0; i < monthRecords.Count; i++)
        {
            DeliveryRecord record = monthRecords[i];

            if (record == null || string.IsNullOrWhiteSpace(record.Date))
            {
                continue;
            }

            if (!DateTime.TryParse(record.Date, out DateTime parsedDate))
            {
                continue;
            }

            int weekIndex = (firstDayOfWeek + parsedDate.Day - 1) / 7;
            int value = GetRecordAmountBySelectedItem(record);

            if (weekIndex >= 0 && weekIndex < weeklyData.Count)
            {
                weeklyData[weekIndex].Value += value;
            }
        }

        return weeklyData;
    }

    private void BuildWeeklyBarChart()
    {
        if (chartContainer == null)
        {
            Debug.LogWarning("[CalendarPanelUI] ChartContainer 참조가 없습니다.");
            return;
        }

        ClearChildren(chartContainer);

        List<WeeklyChartData> chartData = BuildWeeklyChartData();

        if (chartData.Count == 0)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        float containerWidth = chartContainer.rect.width;
        float containerHeight = chartContainer.rect.height;

        if (containerWidth <= 0f)
        {
            containerWidth = 1000f;
        }

        if (containerHeight <= 0f)
        {
            containerHeight = 700f;
        }

        float spacing = 52f;
        float barWidth = (containerWidth - ((chartData.Count - 1) * spacing)) / chartData.Count;
        barWidth = Mathf.Max(96f, barWidth);

        int maxValue = 0;
        for (int i = 0; i < chartData.Count; i++)
        {
            if (chartData[i].Value > maxValue)
            {
                maxValue = chartData[i].Value;
            }
        }

        if (maxValue <= 0)
        {
            maxValue = 1;
        }

        for (int i = 0; i < chartData.Count; i++)
        {
            CreateWeeklyBarItem(chartData[i], maxValue, barWidth, containerHeight, spacing, i);
        }
    }

    private void CreateWeeklyBarItem(
        WeeklyChartData data,
        int maxValue,
        float barWidth,
        float containerHeight,
        float spacing,
        int index)
    {
        GameObject rootObject = new GameObject($"WeekBar_{data.WeekIndex + 1}", typeof(RectTransform));
        rootObject.transform.SetParent(chartContainer, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.sizeDelta = new Vector2(barWidth, containerHeight);
        rootRect.anchoredPosition = new Vector2(index * (barWidth + spacing), 0f);

        float topLabelHeight = 78f;
        float bottomLabelHeight = 72f;
        float availableBarHeight = Mathf.Max(320f, containerHeight - topLabelHeight - bottomLabelHeight - 36f);

        GameObject valueTextObject = new GameObject("Txt_Value", typeof(RectTransform));
        valueTextObject.transform.SetParent(rootObject.transform, false);

        RectTransform valueRect = valueTextObject.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 1f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.pivot = new Vector2(0.5f, 1f);
        valueRect.sizeDelta = new Vector2(0f, topLabelHeight);
        valueRect.anchoredPosition = new Vector2(0f, 0f);

        TextMeshProUGUI valueTmp = valueTextObject.AddComponent<TextMeshProUGUI>();
        valueTmp.font = GetCalendarFontAsset(valueTmp);
        valueTmp.text = data.Value > 0 ? data.Value.ToString("N0") : "0";
        valueTmp.fontSize = 42f;
        valueTmp.alignment = TextAlignmentOptions.Center;
        valueTmp.color = Color.black;
        valueTmp.raycastTarget = false;
        valueTmp.textWrappingMode = TextWrappingModes.NoWrap;

        GameObject barObject = new GameObject("BarFill", typeof(RectTransform));
        barObject.transform.SetParent(rootObject.transform, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);

        float normalized = Mathf.Clamp01((float)data.Value / maxValue);
        float barHeight = data.Value > 0
            ? Mathf.Lerp(84f, availableBarHeight, normalized)
            : 42f;

        barRect.sizeDelta = new Vector2(0f, barHeight);
        barRect.anchoredPosition = new Vector2(0f, bottomLabelHeight);

        Image barImage = barObject.AddComponent<Image>();
        barImage.color = data.Value > 0 ? chartBarColor : chartZeroBarColor;

        if (selectedDate.HasValue)
        {
            int selectedWeekIndex = GetSelectedWeekIndex(selectedDate.Value);
            if (selectedWeekIndex == data.WeekIndex)
            {
                barImage.color = chartHighlightColor;
            }
        }

        GameObject labelObject = new GameObject("Txt_WeekLabel", typeof(RectTransform));
        labelObject.transform.SetParent(rootObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(0f, bottomLabelHeight);
        labelRect.anchoredPosition = new Vector2(0f, 0f);

        TextMeshProUGUI labelTmp = labelObject.AddComponent<TextMeshProUGUI>();
        labelTmp.font = GetCalendarFontAsset(labelTmp);
        labelTmp.text = data.Label;
        labelTmp.fontSize = 40f;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = Color.black;
        labelTmp.raycastTarget = false;
        labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private int GetSelectedWeekIndex(DateTime date)
    {
        DateTime firstDay = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        int firstDayOfWeek = (int)firstDay.DayOfWeek;
        return (firstDayOfWeek + date.Day - 1) / 7;
    }

    private void RefreshChartGuide()
    {
        if (txtChartGuide != null)
        {
            txtChartGuide.text = $"{displayedMonth:yyyy년 M월} {selectedAnalysisLabel} 주차별 막대그래프";
        }
    }

    private void OnClickPrevMonth()
    {
        displayedMonth = displayedMonth.AddMonths(-1);
        selectedDate = null;
        RefreshCalendarView();
    }

    private void OnClickNextMonth()
    {
        displayedMonth = displayedMonth.AddMonths(1);
        selectedDate = null;
        RefreshCalendarView();
    }

    private void OnClickDayCell(DateTime date)
    {
        selectedDate = date.Date;
        RefreshCalendarView();

        if (deliveryListPanelUI != null)
        {
            deliveryListPanelUI.OpenBySpecificDate(date.Date);
        }

        if (deliveryListPanel != null)
        {
            deliveryListPanel.SetActive(true);
        }

        if (calendarPanel != null)
        {
            calendarPanel.SetActive(false);
        }
    }

    private int GetRecordAmountBySelectedItem(DeliveryRecord record)
    {
        if (record == null)
        {
            return 0;
        }

        switch (selectedAnalysisItemIndex)
        {
            case 0: return record.GrossRevenue;        // 총수입
            case 1: return record.TotalExpense;        // 총비용
            case 2: return record.NetRevenue;          // 실수령액
            case 3: return record.BaseFee;             // 기본요금
            case 4: return record.BaseFeeVatSource;    // 기본요금 + 부가세
            case 5: return record.VatAmount;           // 부가세
            case 6: return record.ExtraFee;            // 추가요금
            case 7: return record.CommissionFee;       // 회사입금
            case 8: return record.DataUsageFee;        // 데이터 사용료
            case 9: return record.OtherDeduction;      // 차감액
            case 10: return record.ElectricCharge;     // 차량전기비
            case 11: return record.TollFee;            // 통행료
        }

        return record.GrossRevenue;
    }

    private Color GetDayCellColor(DateTime date)
    {
        if (selectedDate.HasValue && selectedDate.Value.Date == date.Date)
        {
            return selectedDayCellColor;
        }

        if (DateTime.Now.Date == date.Date)
        {
            return todayCellColor;
        }

        return currentMonthCellColor;
    }

    private TMP_FontAsset GetCalendarFontAsset(TextMeshProUGUI fallbackText)
    {
        if (calendarFontAsset != null)
        {
            return calendarFontAsset;
        }

        if (txtCurrentMonth != null && txtCurrentMonth.font != null)
        {
            return txtCurrentMonth.font;
        }

        if (txtSelectedDayCount != null && txtSelectedDayCount.font != null)
        {
            return txtSelectedDayCount.font;
        }

        return fallbackText != null ? fallbackText.font : null;
    }

    private string FormatCalendarWorkedTime(int totalMinutes)
    {
        int safeMinutes = Mathf.Max(0, totalMinutes);
        int hour = safeMinutes / 60;
        int minute = safeMinutes % 60;
        return $"{hour}:{minute:00}";
    }

    private CalendarDaySummary BuildMonthSummary(Dictionary<int, CalendarDaySummary> daySummaryMap)
    {
        CalendarDaySummary monthSummary = new CalendarDaySummary();

        if (daySummaryMap == null)
        {
            return monthSummary;
        }

        foreach (KeyValuePair<int, CalendarDaySummary> pair in daySummaryMap)
        {
            CalendarDaySummary daySummary = pair.Value;

            if (daySummary == null)
            {
                continue;
            }

            monthSummary.WorkedMinutes += daySummary.WorkedMinutes;
            monthSummary.DeliveryCount += daySummary.DeliveryCount;
            monthSummary.GrossRevenue += daySummary.GrossRevenue;
            monthSummary.TotalExpense += daySummary.TotalExpense;
            monthSummary.NetRevenue += daySummary.NetRevenue;
            monthSummary.VatAmount += daySummary.VatAmount;
        }

        return monthSummary;
    }

    private bool IsCountableDeliveryRecord(DeliveryRecord record)
    {
        if (record == null)
        {
            return false;
        }

        if (record.IsDailyFixedExpense)
        {
            return false;
        }

        return record.BaseFee > 0 || record.BaseFeeVatSource > 0 || record.ExtraFee > 0;
    }

    private int GetMonthDeliveryCount()
    {
        if (deliveryManager == null)
        {
            return 0;
        }

        DateTime firstDay = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        DateTime lastDay = new DateTime(displayedMonth.Year, displayedMonth.Month, DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month));
        List<DeliveryRecord> monthRecords = deliveryManager.GetRecordsByDateRange(firstDay, lastDay);

        int count = 0;

        for (int i = 0; i < monthRecords.Count; i++)
        {
            DeliveryRecord record = monthRecords[i];

            if (record == null)
            {
                continue;
            }

            bool isDeliveryCount = IsCountableDeliveryRecord(record);
            if (isDeliveryCount)
            {
                count += 1;
            }
        }

        return count;
    }

    private int GetMonthGrossAmount()
    {
        if (deliveryManager == null)
        {
            return 0;
        }

        DateTime firstDay = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        DateTime lastDay = new DateTime(displayedMonth.Year, displayedMonth.Month, DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month));
        List<DeliveryRecord> monthRecords = deliveryManager.GetRecordsByDateRange(firstDay, lastDay);

        int total = 0;
        for (int i = 0; i < monthRecords.Count; i++)
        {
            DeliveryRecord record = monthRecords[i];
            if (record == null)
            {
                continue;
            }

            total += record.GrossRevenue;
        }

        return total;
    }

    private int GetMonthWorkedMinutes()
    {
        if (workSessionManager == null)
        {
            return 0;
        }

        int daysInMonth = DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month);
        int total = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime targetDate = new DateTime(displayedMonth.Year, displayedMonth.Month, day);
            total += workSessionManager.GetWorkedMinutesByDate(targetDate);
        }

        return total;
    }

    private void ClearChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        List<GameObject> deleteTargets = new List<GameObject>();

        for (int i = 0; i < root.childCount; i++)
        {
            deleteTargets.Add(root.GetChild(i).gameObject);
        }

        for (int i = 0; i < deleteTargets.Count; i++)
        {
            Destroy(deleteTargets[i]);
        }
    }
}
