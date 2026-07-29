using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DeliveryListPanelUI : MonoBehaviour
{
    private enum RecordFilterType
    {
        Today = 0,
        Week = 1,
        Month = 2,
        VatMonth = 3
    }

    [Header("Dependencies")]
    [SerializeField] private DeliveryManager deliveryManager;

    [Header("Linked UI")]
    [SerializeField] private DeliveryInputPanelUI deliveryInputPanelUI;
    [SerializeField] private HomePanelUI homePanelUI;
    [SerializeField] private StatsPanelUI statsPanelUI;
    [SerializeField] private SettlementPanelUI settlementPanelUI;

    [Header("Panel References")]
    [SerializeField] private GameObject deliveryListPanel;
    [SerializeField] private GameObject deliveryInputPanel;

    [Header("List UI")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_Text txtEmpty;
    [SerializeField] private TMP_FontAsset listFontAsset;

    [Header("Filter Buttons")]
    [SerializeField] private Button btnFilterToday;
    [SerializeField] private Button btnFilterWeek;
    [SerializeField] private Button btnFilterMonth;
    [SerializeField] private Button btnFilterVat;

    [Header("Optional Colors")]
    [SerializeField] private Color dateHeaderBackgroundColor = new Color(0.92f, 0.90f, 0.84f, 1f);
    [SerializeField] private Color itemBackgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color itemTextColor = Color.black;
    [SerializeField] private Color editButtonColor = new Color(0.86f, 0.93f, 1f, 1f);
    [SerializeField] private Color deleteButtonColor = new Color(1f, 0.88f, 0.88f, 1f);

    [Header("List Performance")]
    [SerializeField] private int maxVisibleRecords = 300;

    private RecordFilterType currentFilter = RecordFilterType.Today;
    private bool useSpecificDateFilter = false;
    private DateTime specificDateFilter = DateTime.MinValue;
    private DateTime vatDisplayMonth = DateTime.MinValue;

    private void Awake()
    {
        TryInitializeDependencies();
        RegisterEvents();
    }

    private void OnEnable()
    {
        RefreshList();
    }

    private void TryInitializeDependencies()
    {
        if (deliveryManager == null)
        {
            deliveryManager = FindFirstObjectByType<DeliveryManager>();
        }
    }

    private void RegisterEvents()
    {
        if (btnFilterToday != null)
        {
            btnFilterToday.onClick.AddListener(SetFilterToday);
        }

        if (btnFilterWeek != null)
        {
            btnFilterWeek.onClick.AddListener(SetFilterWeek);
        }

        if (btnFilterMonth != null)
        {
            btnFilterMonth.onClick.AddListener(SetFilterMonth);
        }

        if (btnFilterVat != null)
        {
            btnFilterVat.onClick.AddListener(SetFilterVatMonth);
        }
    }

    private void SetFilterToday()
    {
        useSpecificDateFilter = false;
        currentFilter = RecordFilterType.Today;
        RefreshList();
    }

    private void SetFilterWeek()
    {
        useSpecificDateFilter = false;
        currentFilter = RecordFilterType.Week;
        RefreshList();
    }

    private void SetFilterMonth()
    {
        useSpecificDateFilter = false;
        currentFilter = RecordFilterType.Month;
        RefreshList();
    }

    private void SetFilterVatMonth()
    {
        useSpecificDateFilter = false;
        currentFilter = RecordFilterType.VatMonth;

        if (vatDisplayMonth == DateTime.MinValue)
        {
            vatDisplayMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        }

        RefreshList();
    }

    public void OpenBySpecificDate(DateTime targetDate)
    {
        useSpecificDateFilter = true;
        specificDateFilter = targetDate.Date;
        RefreshList();
    }

    public void RefreshList()
    {
        TryInitializeDependencies();

        if (deliveryManager == null)
        {
            Debug.LogWarning("[DeliveryListPanelUI] DeliveryManager 참조가 없습니다.");
            return;
        }

        if (contentRoot == null)
        {
            Debug.LogWarning("[DeliveryListPanelUI] Content Root 참조가 없습니다.");
            return;
        }

        ClearGeneratedItems();

        List<DeliveryRecord> records = GetRecordsByCurrentFilter();
        records = SortRecordsNewestFirst(records);

        if (currentFilter == RecordFilterType.VatMonth)
        {
            if (txtEmpty != null)
            {
                txtEmpty.gameObject.SetActive(false);
            }

            records = LimitVisibleRecords(records);

            CreateVatSummaryItem(records);
            CreateVatTableHeaderItem();

            for (int i = 0; i < records.Count; i++)
            {
                CreateVatRecordItem(records[i], i);
            }

            if ((records == null || records.Count == 0) && txtEmpty != null)
            {
                txtEmpty.gameObject.SetActive(true);
                txtEmpty.text = $"{GetVatDisplayMonth():yyyy년 M월} 부가세 기록이 없습니다";
            }

            return;
        }

        records = LimitVisibleRecords(records);

        if (records == null || records.Count == 0)
        {
            if (txtEmpty != null)
            {
                txtEmpty.gameObject.SetActive(true);
                txtEmpty.text = useSpecificDateFilter
                    ? "선택한 날짜 기록이 없습니다"
                    : "저장된 기록이 없습니다";
            }

            return;
        }

        if (txtEmpty != null)
        {
            txtEmpty.gameObject.SetActive(false);
        }

        string previousDateText = string.Empty;

        for (int i = 0; i < records.Count; i++)
        {
            DeliveryRecord record = records[i];
            string currentDateText = GetDisplayDateText(record);

            if (previousDateText != currentDateText)
            {
                CreateDateHeaderItem(currentDateText, i);
                previousDateText = currentDateText;
            }

            CreateRecordItem(record, i);
        }
    }

    private List<DeliveryRecord> GetRecordsByCurrentFilter()
    {
        if (useSpecificDateFilter)
        {
            return deliveryManager.GetRecordsByDateRange(specificDateFilter, specificDateFilter);
        }

        if (currentFilter == RecordFilterType.Today)
        {
            return deliveryManager.GetTodayRecords();
        }

        if (currentFilter == RecordFilterType.Month)
        {
            return deliveryManager.GetCurrentMonthRecords();
        }

        if (currentFilter == RecordFilterType.VatMonth)
        {
            return GetVatRecordsByDisplayedMonth();
        }

        List<DeliveryRecord> sourceRecords = deliveryManager.GetAllRecords();
        List<DeliveryRecord> weekRecords = new List<DeliveryRecord>();

        DateTime now = DateTime.Now.Date;
        DateTime minDate = now.AddDays(-6);

        for (int i = 0; i < sourceRecords.Count; i++)
        {
            DeliveryRecord record = sourceRecords[i];

            if (record == null || string.IsNullOrWhiteSpace(record.Date))
            {
                continue;
            }

            bool isParsed = DateTime.TryParse(record.Date, out DateTime parsedDate);

            if (!isParsed)
            {
                continue;
            }

            DateTime onlyDate = parsedDate.Date;

            if (onlyDate >= minDate && onlyDate <= now)
            {
                weekRecords.Add(record);
            }
        }

        return weekRecords;
    }

    private List<DeliveryRecord> GetVatRecordsByDisplayedMonth()
    {
        DateTime displayMonth = GetVatDisplayMonth();

        DateTime firstDay = new DateTime(displayMonth.Year, displayMonth.Month, 1);
        DateTime lastDay = new DateTime(
            displayMonth.Year,
            displayMonth.Month,
            DateTime.DaysInMonth(displayMonth.Year, displayMonth.Month));

        List<DeliveryRecord> monthRecords = deliveryManager.GetRecordsByDateRange(firstDay, lastDay);
        List<DeliveryRecord> vatRecords = new List<DeliveryRecord>();

        for (int i = 0; i < monthRecords.Count; i++)
        {
            DeliveryRecord record = monthRecords[i];

            if (record == null)
            {
                continue;
            }

            if (record.IsDailyFixedExpense)
            {
                continue;
            }

            if (record.BaseFeeVatSource > 0 || record.VatAmount > 0)
            {
                vatRecords.Add(record);
            }
        }

        return vatRecords;
    }

    private DateTime GetVatDisplayMonth()
    {
        if (vatDisplayMonth == DateTime.MinValue)
        {
            vatDisplayMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        }

        return vatDisplayMonth;
    }

    private void MoveVatMonth(int monthOffset)
    {
        vatDisplayMonth = GetVatDisplayMonth().AddMonths(monthOffset);
        RefreshList();
    }

    private void CreateVatSummaryItem(List<DeliveryRecord> records)
    {
        DateTime displayMonth = GetVatDisplayMonth();

        int totalBaseFeeVatSource = 0;
        int totalVatAmount = 0;
        int count = 0;

        if (records != null)
        {
            for (int i = 0; i < records.Count; i++)
            {
                DeliveryRecord record = records[i];

                if (record == null)
                {
                    continue;
                }

                totalBaseFeeVatSource += Mathf.Max(0, record.BaseFeeVatSource);
                totalVatAmount += Mathf.Max(0, record.VatAmount);
                count += 1;
            }
        }

        int totalAmount = totalBaseFeeVatSource + totalVatAmount;

        GameObject itemRoot = new GameObject("VatSummaryItem", typeof(RectTransform));
        itemRoot.transform.SetParent(contentRoot, false);

        LayoutElement layoutElement = itemRoot.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 118f;

        Image backgroundImage = itemRoot.AddComponent<Image>();
        backgroundImage.color = dateHeaderBackgroundColor;
        backgroundImage.raycastTarget = false;

        CreateVatEdgeButton(
            itemRoot.transform,
            "Btn_PrevVatMonth",
            "이전달",
            0.01f,
            0.16f,
            0.62f,
            0.96f,
            editButtonColor,
            20f,
            delegate { MoveVatMonth(-1); }
        );

        CreateVatEdgeButton(
            itemRoot.transform,
            "Btn_NextVatMonth",
            "다음달",
            0.84f,
            0.99f,
            0.62f,
            0.96f,
            editButtonColor,
            20f,
            delegate { MoveVatMonth(1); }
        );

        CreateVatAnchoredText(
            itemRoot.transform,
            "Txt_VatMonthTitle",
            $"{displayMonth:yyyy년 M월} 부가세 내역",
            0.18f,
            0.82f,
            0.64f,
            0.96f,
            40f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            TextOverflowModes.Overflow
        );

        CreateVatAnchoredText(
            itemRoot.transform,
            "Txt_VatSummary",
            $"요금: {totalBaseFeeVatSource:N0} | 부가세: {totalVatAmount:N0} | 총합: {totalAmount:N0}원 | 건수: {count}건",
            0.01f,
            0.99f,
            0.18f,
            0.52f,
            30f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            TextOverflowModes.Overflow
        );
    }

    private void CreateVatTableHeaderItem()
    {
        GameObject itemRoot = new GameObject("VatTableHeaderItem", typeof(RectTransform));
        itemRoot.transform.SetParent(contentRoot, false);

        LayoutElement layoutElement = itemRoot.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 38f;

        Image backgroundImage = itemRoot.AddComponent<Image>();
        backgroundImage.color = new Color(0.90f, 0.90f, 0.86f, 1f);
        backgroundImage.raycastTarget = false;

        CreateVatAnchoredText(itemRoot.transform, "Txt_HeaderEdit", "", 0.00f, 0.10f, 0f, 1f, 30f, FontStyles.Bold, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_HeaderDate", "날짜", 0.10f, 0.19f, 0f, 1f, 30f, FontStyles.Bold, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_HeaderStart", "출발지", 0.19f, 0.32f, 0f, 1f, 30f, FontStyles.Bold, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_HeaderEnd", "도착지", 0.32f, 0.45f, 0f, 1f, 30f, FontStyles.Bold, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_HeaderFee", "요금", 0.45f, 0.60f, 0f, 1f, 30f, FontStyles.Bold, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_HeaderVat", "부가세", 0.60f, 0.75f, 0f, 1f, 30f, FontStyles.Bold, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_HeaderTotal", "합계", 0.75f, 0.90f, 0f, 1f, 30f, FontStyles.Bold, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_HeaderDelete", "", 0.90f, 1.00f, 0f, 1f, 30f, FontStyles.Bold, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
    }

    private void CreateVatRecordItem(DeliveryRecord record, int index)
    {
        if (record == null)
        {
            return;
        }

        string recordId = record.Id;

        GameObject itemRoot = new GameObject($"VatRecordItem_{index}", typeof(RectTransform));
        itemRoot.transform.SetParent(contentRoot, false);

        LayoutElement layoutElement = itemRoot.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 42f;

        Image backgroundImage = itemRoot.AddComponent<Image>();
        backgroundImage.color = itemBackgroundColor;
        backgroundImage.raycastTarget = false;

        string startAddress = string.IsNullOrWhiteSpace(record.StartAddress) ? "미입력" : record.StartAddress;
        string endAddress = string.IsNullOrWhiteSpace(record.EndAddress) ? "미입력" : record.EndAddress;
        string dateText = GetVatShortDateText(record);

        int fee = Mathf.Max(0, record.BaseFeeVatSource);
        int vat = Mathf.Max(0, record.VatAmount);
        int total = fee + vat;

        CreateVatEdgeButton(
            itemRoot.transform,
            "Btn_Edit",
            "수정",
            0.00f,
            0.10f,
            0.12f,
            0.88f,
            editButtonColor,
            18f,
            delegate { OnClickEditRecord(recordId); }
        );

        CreateVatAnchoredText(itemRoot.transform, "Txt_Date", dateText, 0.10f, 0.19f, 0f, 1f, 30f, FontStyles.Normal, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_Start", startAddress, 0.19f, 0.32f, 0f, 1f, 30f, FontStyles.Normal, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_End", endAddress, 0.32f, 0.45f, 0f, 1f, 30f, FontStyles.Normal, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_Fee", $"{fee:N0}", 0.45f, 0.60f, 0f, 1f, 30f, FontStyles.Normal, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_Vat", $"{vat:N0}", 0.60f, 0.75f, 0f, 1f, 30f, FontStyles.Normal, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        CreateVatAnchoredText(itemRoot.transform, "Txt_Total", $"{total:N0}원", 0.75f, 0.90f, 0f, 1f, 30f, FontStyles.Normal, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);

        CreateVatEdgeButton(
            itemRoot.transform,
            "Btn_Delete",
            "삭제",
            0.90f,
            1.00f,
            0.12f,
            0.88f,
            deleteButtonColor,
            18f,
            delegate { OnClickDeleteRecord(recordId); }
        );
    }

    private void CreateVatSmallButton(
        Transform parent,
        string objectName,
        string label,
        Color backgroundColor,
        float preferredWidth,
        UnityAction onClickAction)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 68f;
        layoutElement.flexibleWidth = 0f;
        layoutElement.preferredHeight = 38f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        if (onClickAction != null)
        {
            button.onClick.AddListener(onClickAction);
        }

        GameObject textObject = new GameObject("Txt_ButtonLabel", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textObject.AddComponent<TextMeshProUGUI>();

        if (listFontAsset != null)
        {
            buttonText.font = listFontAsset;
        }

        buttonText.text = label;
        buttonText.fontSize = 50f;
        buttonText.color = itemTextColor;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.raycastTarget = false;
    }

    private string GetVatShortDateText(DeliveryRecord record)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.Date))
        {
            return "--";
        }

        bool isParsed = DateTime.TryParseExact(
            record.Date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate);

        if (!isParsed)
        {
            return record.Date;
        }

        return $"{parsedDate.Month}.{parsedDate.Day}";
    }

    private void ClearGeneratedItems()
    {
        List<GameObject> deleteTargets = new List<GameObject>();

        for (int i = 0; i < contentRoot.childCount; i++)
        {
            Transform child = contentRoot.GetChild(i);

            if (txtEmpty != null && child == txtEmpty.transform)
            {
                continue;
            }

            deleteTargets.Add(child.gameObject);
        }

        for (int i = 0; i < deleteTargets.Count; i++)
        {
            Destroy(deleteTargets[i]);
        }
    }

    private List<DeliveryRecord> SortRecordsNewestFirst(List<DeliveryRecord> sourceRecords)
    {
        if (sourceRecords == null)
        {
            return new List<DeliveryRecord>();
        }

        List<DeliveryRecord> sortedRecords = new List<DeliveryRecord>(sourceRecords);

        sortedRecords.Sort((a, b) =>
        {
            DateTime aDateTime = ParseRecordDateTime(a);
            DateTime bDateTime = ParseRecordDateTime(b);
            return bDateTime.CompareTo(aDateTime);
        });

        return sortedRecords;
    }

    private List<DeliveryRecord> LimitVisibleRecords(List<DeliveryRecord> sourceRecords)
    {
        if (sourceRecords == null)
        {
            return new List<DeliveryRecord>();
        }

        if (maxVisibleRecords <= 0 || sourceRecords.Count <= maxVisibleRecords)
        {
            return sourceRecords;
        }

        return sourceRecords.GetRange(0, maxVisibleRecords);
    }

    private DateTime ParseRecordDateTime(DeliveryRecord record)
    {
        if (record == null)
        {
            return DateTime.MinValue;
        }

        string dateText = string.IsNullOrWhiteSpace(record.Date) ? "1900-01-01" : record.Date;
        string timeText = string.IsNullOrWhiteSpace(record.EndTime) ? "00:00" : record.EndTime;
        string combined = $"{dateText} {timeText}";

        bool isParsed = DateTime.TryParseExact(
            combined,
            "yyyy-MM-dd HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDateTime);

        return isParsed ? parsedDateTime : DateTime.MinValue;
    }

    private string GetDisplayDateText(DeliveryRecord record)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.Date))
        {
            return "날짜 없음";
        }

        bool isParsed = DateTime.TryParseExact(
            record.Date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate);

        return isParsed ? parsedDate.ToString("yyyy-MM-dd") : record.Date;
    }

    private string GetDisplayDateTimeText(DeliveryRecord record)
    {
        string dateText = GetDisplayDateText(record);
        string timeText = string.IsNullOrWhiteSpace(record != null ? record.EndTime : string.Empty)
            ? "--:--"
            : record.EndTime;

        return $"{dateText} {timeText}";
    }

    private void CreateDateHeaderItem(string dateText, int index)
    {
        GameObject headerRoot = new GameObject($"DateHeader_{index}", typeof(RectTransform));
        headerRoot.transform.SetParent(contentRoot, false);

        LayoutElement layoutElement = headerRoot.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 64f;

        Image image = headerRoot.AddComponent<Image>();
        image.color = dateHeaderBackgroundColor;
        image.raycastTarget = false;

        HorizontalLayoutGroup layoutGroup = headerRoot.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.padding = new RectOffset(20, 16, 10, 10);
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = true;

        GameObject textObject = new GameObject("Txt_DateHeader", typeof(RectTransform));
        textObject.transform.SetParent(headerRoot.transform, false);

        TextMeshProUGUI headerText = textObject.AddComponent<TextMeshProUGUI>();

        if (listFontAsset != null)
        {
            headerText.font = listFontAsset;
        }

        headerText.text = dateText;
        headerText.fontSize = 40f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = itemTextColor;
        headerText.alignment = TextAlignmentOptions.Left;
        headerText.raycastTarget = false;
    }

    private void CreateRecordItem(DeliveryRecord record, int index)
    {
        if (record == null)
        {
            return;
        }

        string recordId = record.Id;

        GameObject itemRoot = new GameObject($"RecordItem_{index}", typeof(RectTransform));
        itemRoot.transform.SetParent(contentRoot, false);

        LayoutElement layoutElement = itemRoot.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 120f;

        Image backgroundImage = itemRoot.AddComponent<Image>();
        backgroundImage.color = itemBackgroundColor;
        backgroundImage.raycastTarget = false;

        VerticalLayoutGroup verticalLayoutGroup = itemRoot.AddComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.padding = new RectOffset(16, 16, 12, 12);
        verticalLayoutGroup.spacing = 30f;
        verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        verticalLayoutGroup.childControlWidth = true;
        verticalLayoutGroup.childControlHeight = false;
        verticalLayoutGroup.childForceExpandWidth = true;
        verticalLayoutGroup.childForceExpandHeight = false;

        GameObject textObject = new GameObject("Txt_RecordInfo", typeof(RectTransform));
        textObject.transform.SetParent(itemRoot.transform, false);

        LayoutElement textLayout = textObject.AddComponent<LayoutElement>();
        textLayout.preferredHeight = 52f;

        TextMeshProUGUI recordText = textObject.AddComponent<TextMeshProUGUI>();

        if (listFontAsset != null)
        {
            recordText.font = listFontAsset;
        }

        recordText.fontSize = 32f;
        recordText.color = itemTextColor;
        recordText.alignment = TextAlignmentOptions.TopLeft;
        recordText.textWrappingMode = TextWrappingModes.Normal;
        recordText.raycastTarget = false;
        recordText.lineSpacing = -8f;

        string startAddress = string.IsNullOrWhiteSpace(record.StartAddress) ? "미입력" : record.StartAddress;
        string endAddress = string.IsNullOrWhiteSpace(record.EndAddress) ? "미입력" : record.EndAddress;
        string dateTimeText = GetDisplayDateTimeText(record);
        string memoText = string.IsNullOrWhiteSpace(record.Memo) ? string.Empty : $"메모:{record.Memo} | ";

        recordText.text =
            $"{dateTimeText} | {startAddress} → {endAddress}\n" +
            $"{memoText}{BuildRecordAmountLine(record)}";

        GameObject buttonRow = new GameObject("ButtonRow", typeof(RectTransform));
        buttonRow.transform.SetParent(itemRoot.transform, false);

        LayoutElement rowLayout = buttonRow.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 14f;

        HorizontalLayoutGroup horizontalLayoutGroup = buttonRow.AddComponent<HorizontalLayoutGroup>();
        horizontalLayoutGroup.spacing = 14f;
        horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        horizontalLayoutGroup.childControlWidth = true;
        horizontalLayoutGroup.childControlHeight = true;
        horizontalLayoutGroup.childForceExpandWidth = true;
        horizontalLayoutGroup.childForceExpandHeight = true;

        CreateActionButton(
            buttonRow.transform,
            "Btn_Edit",
            "수정",
            editButtonColor,
            delegate { OnClickEditRecord(recordId); }
        );

        CreateActionButton(
            buttonRow.transform,
            "Btn_Delete",
            "삭제",
            deleteButtonColor,
            delegate { OnClickDeleteRecord(recordId); }
        );
    }

    private string BuildRecordAmountLine(DeliveryRecord record)
    {
        if (record == null)
        {
            return string.Empty;
        }

        if (record.IsDailyFixedExpense)
        {
            return $"총비용 {record.TotalExpense:N0}원 / 실수령액 {record.NetRevenue:N0}원";
        }

        string amountLine = $"총수입 {record.GrossRevenue:N0}원 / 실수령액 {record.NetRevenue:N0}원";

        if (record.VatAmount > 0)
        {
            amountLine += $" / 부가세 {record.VatAmount:N0}원";
        }

        return amountLine;
    }

    private void CreateActionButton(
        Transform parent,
        string objectName,
        string label,
        Color backgroundColor,
        UnityAction onClickAction)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 15f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        if (onClickAction != null)
        {
            button.onClick.AddListener(onClickAction);
        }

        GameObject textObject = new GameObject("Txt_ButtonLabel", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textObject.AddComponent<TextMeshProUGUI>();

        if (listFontAsset != null)
        {
            buttonText.font = listFontAsset;
        }

        buttonText.text = label;
        buttonText.fontSize = 32f;
        buttonText.color = itemTextColor;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.raycastTarget = false;
    }

    private void CreateVatAnchoredText(
        Transform parent,
        string objectName,
        string text,
        float anchorMinX,
        float anchorMaxX,
        float anchorMinY,
        float anchorMaxY,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        TextOverflowModes overflowMode)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rect.offsetMin = new Vector2(2f, 1f);
        rect.offsetMax = new Vector2(-2f, -1f);

        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();

        if (listFontAsset != null)
        {
            textComponent.font = listFontAsset;
        }

        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = itemTextColor;
        textComponent.alignment = alignment;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        textComponent.overflowMode = overflowMode;
        textComponent.raycastTarget = false;
    }

    private void CreateVatEdgeButton(
        Transform parent,
        string objectName,
        string label,
        float anchorMinX,
        float anchorMaxX,
        float anchorMinY,
        float anchorMaxY,
        Color backgroundColor,
        float fontSize,
        UnityAction onClickAction)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(anchorMinX, anchorMinY);
        buttonRect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        buttonRect.offsetMin = new Vector2(2f, 2f);
        buttonRect.offsetMax = new Vector2(-2f, -2f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        if (onClickAction != null)
        {
            button.onClick.AddListener(onClickAction);
        }

        GameObject textObject = new GameObject("Txt_ButtonLabel", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textObject.AddComponent<TextMeshProUGUI>();

        if (listFontAsset != null)
        {
            buttonText.font = listFontAsset;
        }

        buttonText.text = label;
        buttonText.fontSize = 30f;
        buttonText.color = itemTextColor;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.raycastTarget = false;
    }

    private void OnClickEditRecord(string recordId)
    {
        if (deliveryManager == null)
        {
            Debug.LogWarning("[DeliveryListPanelUI] DeliveryManager 참조가 없습니다.");
            return;
        }

        if (deliveryInputPanelUI == null)
        {
            Debug.LogWarning("[DeliveryListPanelUI] DeliveryInputPanelUI 참조가 없습니다.");
            return;
        }

        DeliveryRecord record = deliveryManager.GetRecordById(recordId);

        if (record == null)
        {
            Debug.LogWarning("[DeliveryListPanelUI] 수정 대상 record를 찾지 못했습니다.");
            return;
        }

        deliveryInputPanelUI.BeginEditRecord(record);

        if (deliveryInputPanel != null)
        {
            deliveryInputPanel.SetActive(true);
        }

        if (deliveryListPanel != null)
        {
            deliveryListPanel.SetActive(false);
        }
    }

    private void OnClickDeleteRecord(string recordId)
    {
        if (deliveryManager == null)
        {
            Debug.LogWarning("[DeliveryListPanelUI] DeliveryManager 참조가 없습니다.");
            return;
        }

        bool isDeleted = deliveryManager.DeleteDeliveryRecord(recordId);

        if (!isDeleted)
        {
            Debug.LogWarning("[DeliveryListPanelUI] 삭제 실패");
            return;
        }

        RefreshList();
        RefreshLinkedPanels();
    }

    private void RefreshLinkedPanels()
    {
        if (homePanelUI != null)
        {
            homePanelUI.RefreshHomeSummary();
        }

        if (statsPanelUI != null)
        {
            statsPanelUI.ShowTodayStats();
            statsPanelUI.ShowMonthStats();
        }

        if (settlementPanelUI != null)
        {
            settlementPanelUI.RefreshSettlementUI();
        }
    }
}
