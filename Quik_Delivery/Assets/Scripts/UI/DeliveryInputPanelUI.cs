using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryInputPanelUI : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private RevenueCalculator revenueCalculator;
    [SerializeField] private DeliveryManager deliveryManager;

    [Header("Linked UI")]
    [SerializeField] private HomePanelUI homePanelUI;
    [SerializeField] private DeliveryListPanelUI deliveryListPanelUI;
    [SerializeField] private StatsPanelUI statsPanelUI;
    [SerializeField] private SettlementPanelUI settlementPanelUI;

    [Header("Optional Save Button Label")]
    [SerializeField] private TMP_Text txtSaveButtonLabel;

    [Header("Location Preview Text")]
    [SerializeField] private TMP_Text txtStartLocationPreview;
    [SerializeField] private TMP_Text txtEndLocationPreview;

    [Header("Location Inputs")]
    [SerializeField] private TMP_InputField inputStartAddress;
    [SerializeField] private TMP_InputField inputEndAddress;

    [Header("Record Date Time")]
    [SerializeField] private TMP_InputField inputRecordDate;
    [SerializeField] private TMP_InputField inputRecordTime;

    [Header("Fee Inputs")]
    [SerializeField] private TMP_InputField inputBaseFee;
    [SerializeField] private TMP_InputField inputBaseFeeVatSource;  // 기본요금 + 부가세 입력값
    [SerializeField] private TMP_InputField inputExtraFee;
    [SerializeField] private TMP_InputField inputCommissionFee;     // 회사입금
    [SerializeField] private TMP_InputField inputDataUsageFee;      // 데이터 사용료
    [SerializeField] private TMP_InputField inputOtherDeduction;    // 차감액
    [SerializeField] private TMP_InputField inputElectricCharge;
    [SerializeField] private TMP_InputField inputTollFee;

    [Header("Memo")]
    [SerializeField] private TMP_InputField inputMemo;

    [Header("Result UI")]
    [SerializeField] private TMP_Text txtGrossRevenue;
    [SerializeField] private TMP_Text txtTotalExpense;
    [SerializeField] private TMP_Text txtVatAmount;
    [SerializeField] private TMP_Text txtNetRevenue;

    [Header("Buttons")]
    [SerializeField] private Button btnSaveDelivery;

    [Header("Panel References")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject deliveryInputPanel;
    [SerializeField] private GameObject deliveryListPanel;

    private bool isEditMode = false;
    private DeliveryRecord editingSourceRecord = null;

    private void Awake()
    {
        TryInitializeDependencies();
        RegisterEvents();
        InitializeUI();
        RefreshCalculation();
        UpdateSaveButtonLabel();
    }

    private void TryInitializeDependencies()
    {
        if (deliveryManager == null)
        {
            deliveryManager = FindFirstObjectByType<DeliveryManager>();
        }

        if (homePanelUI == null)
        {
            homePanelUI = FindFirstObjectByType<HomePanelUI>();
        }

        if (txtSaveButtonLabel == null && btnSaveDelivery != null)
        {
            txtSaveButtonLabel = btnSaveDelivery.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void RegisterEvents()
    {
        RegisterInputEvent(inputBaseFee);
        RegisterInputEvent(inputBaseFeeVatSource);
        RegisterInputEvent(inputExtraFee);
        RegisterInputEvent(inputCommissionFee);
        RegisterInputEvent(inputDataUsageFee);
        RegisterInputEvent(inputOtherDeduction);
        RegisterInputEvent(inputElectricCharge);
        RegisterInputEvent(inputTollFee);

        if (inputStartAddress != null)
        {
            inputStartAddress.onValueChanged.AddListener(delegate { RefreshLocationPreviewTexts(); });
        }

        if (inputEndAddress != null)
        {
            inputEndAddress.onValueChanged.AddListener(delegate { RefreshLocationPreviewTexts(); });
        }

        if (inputRecordDate != null)
        {
            inputRecordDate.onEndEdit.RemoveAllListeners();
            inputRecordDate.onEndEdit.AddListener(OnEndEditRecordDate);
        }

        if (inputRecordTime != null)
        {
            inputRecordTime.onEndEdit.RemoveAllListeners();
            inputRecordTime.onEndEdit.AddListener(OnEndEditRecordTime);
        }

        if (btnSaveDelivery != null)
        {
            btnSaveDelivery.onClick.AddListener(OnClickSaveDelivery);
        }
    }

    private void RegisterInputEvent(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.onValueChanged.AddListener(delegate { RefreshCalculation(); });
    }

    private void InitializeUI()
    {
        if (txtGrossRevenue != null && string.IsNullOrWhiteSpace(txtGrossRevenue.text))
        {
            txtGrossRevenue.text = "총수입: 0원";
        }

        if (txtTotalExpense != null && string.IsNullOrWhiteSpace(txtTotalExpense.text))
        {
            txtTotalExpense.text = "총비용: 0원";
        }

        if (txtVatAmount != null && string.IsNullOrWhiteSpace(txtVatAmount.text))
        {
            txtVatAmount.text = "부가세: 0원";
        }

        if (txtNetRevenue != null && string.IsNullOrWhiteSpace(txtNetRevenue.text))
        {
            txtNetRevenue.text = "실수령액: 0원";
        }

        if (inputRecordDate != null && string.IsNullOrWhiteSpace(inputRecordDate.text))
        {
            inputRecordDate.text = DateTime.Now.ToString("yyyy-MM-dd");
        }

        if (inputRecordTime != null && string.IsNullOrWhiteSpace(inputRecordTime.text))
        {
            inputRecordTime.text = DateTime.Now.ToString("HH:mm");
        }

        RefreshLocationPreviewTexts();
    }

    public void BeginEditRecord(DeliveryRecord sourceRecord)
    {
        if (sourceRecord == null)
        {
            Debug.LogWarning("[DeliveryInputPanelUI] 수정할 sourceRecord가 null입니다.");
            return;
        }

        isEditMode = true;
        editingSourceRecord = sourceRecord;

        ApplyRecordToUI(sourceRecord);
        UpdateSaveButtonLabel();
        RefreshCalculation();
    }

    private void ApplyRecordToUI(DeliveryRecord record)
    {
        if (record == null)
        {
            return;
        }

        if (inputStartAddress != null)
        {
            inputStartAddress.text = record.StartAddress;
        }

        if (inputEndAddress != null)
        {
            inputEndAddress.text = record.EndAddress;
        }

        if (inputRecordDate != null)
        {
            inputRecordDate.text = record.Date;
        }

        if (inputRecordTime != null)
        {
            inputRecordTime.text = string.IsNullOrWhiteSpace(record.EndTime) ? record.StartTime : record.EndTime;
        }

        SetInputFieldValue(inputBaseFee, record.BaseFee);
        SetInputFieldValue(inputExtraFee, record.ExtraFee);
        SetInputFieldValue(inputCommissionFee, record.CommissionFee);
        SetInputFieldValue(inputDataUsageFee, record.DataUsageFee);
        SetInputFieldValue(inputOtherDeduction, record.OtherDeduction);
        SetInputFieldValue(inputElectricCharge, record.ElectricCharge);
        SetInputFieldValue(inputTollFee, record.TollFee);

        if (inputMemo != null)
        {
            inputMemo.text = record.Memo;
        }

        RefreshLocationPreviewTexts();
    }

    private void SetInputFieldValue(TMP_InputField inputField, int value)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.text = value > 0 ? value.ToString() : string.Empty;
    }

    private void UpdateSaveButtonLabel()
    {
        if (txtSaveButtonLabel == null)
        {
            return;
        }

        txtSaveButtonLabel.text = isEditMode ? "수정 저장" : "기록 저장";
    }

    public void RefreshCalculation()
    {
        if (revenueCalculator == null)
        {
            Debug.LogWarning("[DeliveryInputPanelUI] RevenueCalculator 참조가 비어 있습니다.");
            return;
        }

        int baseFee = ParseInputValue(inputBaseFee);
        int baseFeeVatSource = ParseInputValue(inputBaseFeeVatSource);
        int extraFee = ParseInputValue(inputExtraFee);
        int commissionFee = ParseInputValue(inputCommissionFee);
        int dataUsageFee = ParseInputValue(inputDataUsageFee);
        int otherDeduction = ParseInputValue(inputOtherDeduction);
        int electricCharge = ParseInputValue(inputElectricCharge);
        int tollFee = ParseInputValue(inputTollFee);

        int grossRevenue = revenueCalculator.CalculateGrossRevenue(
            baseFee,
            baseFeeVatSource,
            extraFee
        );

        int vatAmount = revenueCalculator.CalculateVatAmount(baseFeeVatSource);

        int totalExpense = revenueCalculator.CalculateTotalExpense(
            commissionFee,
            dataUsageFee,
            otherDeduction,
            electricCharge,
            tollFee
        );

        int netRevenue = revenueCalculator.CalculateNetRevenue(grossRevenue, totalExpense);

        if (txtGrossRevenue != null)
        {
            txtGrossRevenue.text = $"총수입: {grossRevenue:N0}원";
        }

        if (txtTotalExpense != null)
        {
            txtTotalExpense.text = $"총비용: {totalExpense:N0}원";
        }

        if (txtVatAmount != null)
        {
            txtVatAmount.text = $"부가세: {vatAmount:N0}원";
        }

        if (txtNetRevenue != null)
        {
            txtNetRevenue.text = $"실수령액: {netRevenue:N0}원";
        }

        //Debug.Log($"[VatDebug] baseFee={baseFee}, baseFeeVatSource={baseFeeVatSource}, grossRevenue={grossRevenue}, vatAmount={vatAmount}, netRevenue={netRevenue}");
    }

    private void OnClickSaveDelivery()
    {
        if (deliveryManager == null)
        {
            Debug.LogWarning("[DeliveryInputPanelUI] DeliveryManager 참조가 비어 있습니다.");
            return;
        }

        if (inputRecordDate != null)
        {
            inputRecordDate.text = AutoFormatRecordDate(inputRecordDate.text);
        }

        if (inputRecordTime != null)
        {
            inputRecordTime.text = AutoFormatRecordTime(inputRecordTime.text);
        }

        bool wasEditMode = isEditMode;
        DeliveryRecord record = BuildDeliveryRecordFromUI();

        if (wasEditMode)
        {
            bool isUpdated = deliveryManager.UpdateDeliveryRecord(record);

            if (!isUpdated)
            {
                Debug.LogWarning("[DeliveryInputPanelUI] 수정 저장 실패");
                return;
            }
        }
        else
        {
            deliveryManager.SaveDeliveryRecord(record);
        }

        ClearInputUI();
        RefreshCalculation();
        RefreshLinkedPanels();

        if (wasEditMode)
        {
            OpenListPanel();
        }
        else
        {
            OpenHomePanel();
        }
    }

    private DeliveryRecord BuildDeliveryRecordFromUI()
    {
        DeliveryRecord record = new DeliveryRecord();

        record.StartAddress = GetSafeAddressInput(inputStartAddress, "출발지 입력");
        record.EndAddress = GetSafeAddressInput(inputEndAddress, "도착지 입력");
        record.StartLatitude = 0d;
        record.StartLongitude = 0d;
        record.EndLatitude = 0d;
        record.EndLongitude = 0d;

        record.BaseFee = ParseInputValue(inputBaseFee);
        record.BaseFeeVatSource = ParseInputValue(inputBaseFeeVatSource);
        record.ExtraFee = ParseInputValue(inputExtraFee);
        record.CommissionFee = ParseInputValue(inputCommissionFee);
        record.DataUsageFee = ParseInputValue(inputDataUsageFee);
        record.OtherDeduction = ParseInputValue(inputOtherDeduction);
        record.ElectricCharge = ParseInputValue(inputElectricCharge);
        record.TollFee = ParseInputValue(inputTollFee);

        record.Memo = inputMemo != null ? inputMemo.text.Trim() : string.Empty;

        string safeDate = GetSafeRecordDateText(isEditMode && editingSourceRecord != null ? editingSourceRecord.Date : DateTime.Now.ToString("yyyy-MM-dd"));
        string safeTime = GetSafeRecordTimeText(isEditMode && editingSourceRecord != null ? editingSourceRecord.EndTime : DateTime.Now.ToString("HH:mm"));

        if (isEditMode && editingSourceRecord != null)
        {
            record.Id = editingSourceRecord.Id;
            record.Date = safeDate;
            record.StartTime = safeTime;
            record.EndTime = safeTime;
            record.IsSettled = editingSourceRecord.IsSettled;
            record.SettlementId = editingSourceRecord.SettlementId;
            record.WorkSessionId = editingSourceRecord.WorkSessionId;
            record.IsDailyFixedExpense = editingSourceRecord.IsDailyFixedExpense;
        }
        else
        {
            record.Date = safeDate;
            record.StartTime = safeTime;
            record.EndTime = safeTime;
            record.IsDailyFixedExpense = false;
        }

        revenueCalculator.ApplyCalculation(record);

        return record;
    }

    private void ClearInputUI()
    {
        if (inputStartAddress != null)
        {
            inputStartAddress.text = string.Empty;
        }

        if (inputEndAddress != null)
        {
            inputEndAddress.text = string.Empty;
        }

        if (inputRecordDate != null)
        {
            inputRecordDate.text = DateTime.Now.ToString("yyyy-MM-dd");
        }

        if (inputRecordTime != null)
        {
            inputRecordTime.text = DateTime.Now.ToString("HH:mm");
        }

        ClearInputField(inputBaseFee);
        ClearInputField(inputExtraFee);
        ClearInputField(inputCommissionFee);
        ClearInputField(inputDataUsageFee);
        ClearInputField(inputOtherDeduction);
        ClearInputField(inputElectricCharge);
        ClearInputField(inputTollFee);
        ClearInputField(inputMemo);

        isEditMode = false;
        editingSourceRecord = null;
        UpdateSaveButtonLabel();
        RefreshLocationPreviewTexts();
    }

    private void ClearInputField(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.text = string.Empty;
    }

    private void RefreshLinkedPanels()
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

        if (settlementPanelUI != null)
        {
            settlementPanelUI.RefreshSettlementUI();
        }
    }

    private void OpenHomePanel()
    {
        if (homePanel != null)
        {
            homePanel.SetActive(true);
        }

        if (deliveryInputPanel != null)
        {
            deliveryInputPanel.SetActive(false);
        }

        if (deliveryListPanel != null)
        {
            deliveryListPanel.SetActive(false);
        }
    }

    private void OpenListPanel()
    {
        if (deliveryListPanel != null)
        {
            deliveryListPanel.SetActive(true);
        }

        if (deliveryInputPanel != null)
        {
            deliveryInputPanel.SetActive(false);
        }

        if (homePanel != null)
        {
            homePanel.SetActive(false);
        }
    }

    private int ParseInputValue(TMP_InputField inputField)
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text))
        {
            return 0;
        }

        string rawText = inputField.text.Trim();

        System.Text.StringBuilder digitBuilder = new System.Text.StringBuilder();

        for (int i = 0; i < rawText.Length; i++)
        {
            if (char.IsDigit(rawText[i]))
            {
                digitBuilder.Append(rawText[i]);
            }
        }

        string digitsOnly = digitBuilder.ToString();

        if (string.IsNullOrWhiteSpace(digitsOnly))
        {
            return 0;
        }

        bool isParsed = int.TryParse(digitsOnly, out int value);

        if (!isParsed)
        {
            return 0;
        }

        return Mathf.Max(0, value);
    }

    private void RefreshLocationPreviewTexts()
    {
        string startAddress = GetSafeAddressInput(inputStartAddress, "출발지 입력");
        string endAddress = GetSafeAddressInput(inputEndAddress, "도착지 입력");

        if (txtStartLocationPreview != null)
        {
            txtStartLocationPreview.text = string.IsNullOrWhiteSpace(startAddress)
                ? "출발지: 미입력"
                : $"출발지: {startAddress}";
        }

        if (txtEndLocationPreview != null)
        {
            txtEndLocationPreview.text = string.IsNullOrWhiteSpace(endAddress)
                ? "도착지: 미입력"
                : $"도착지: {endAddress}";
        }
    }

    private string GetSafeAddressInput(TMP_InputField inputField, params string[] invalidTexts)
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text))
        {
            return string.Empty;
        }

        string value = inputField.text.Trim();

        for (int i = 0; i < invalidTexts.Length; i++)
        {
            if (value == invalidTexts[i])
            {
                return string.Empty;
            }
        }

        return value;
    }

    private string GetSafeRecordDateText(string fallbackDateText)
    {
        string rawText = inputRecordDate != null ? inputRecordDate.text.Trim() : string.Empty;
        string formattedText = AutoFormatRecordDate(rawText);

        bool isParsed = DateTime.TryParseExact(
            formattedText,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate);

        if (isParsed)
        {
            return parsedDate.ToString("yyyy-MM-dd");
        }

        return fallbackDateText;
    }

    private string GetSafeRecordTimeText(string fallbackTimeText)
    {
        string rawText = inputRecordTime != null ? inputRecordTime.text.Trim() : string.Empty;
        string formattedText = AutoFormatRecordTime(rawText);

        bool isParsed = DateTime.TryParseExact(
            formattedText,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedTime);

        if (isParsed)
        {
            return parsedTime.ToString("HH:mm");
        }

        return string.IsNullOrWhiteSpace(fallbackTimeText)
            ? DateTime.Now.ToString("HH:mm")
            : fallbackTimeText;
    }

    private void OnEndEditRecordDate(string rawText)
    {
        if (inputRecordDate == null)
        {
            return;
        }

        inputRecordDate.text = AutoFormatRecordDate(rawText);
    }

    private void OnEndEditRecordTime(string rawText)
    {
        if (inputRecordTime == null)
        {
            return;
        }

        inputRecordTime.text = AutoFormatRecordTime(rawText);
    }

    private string AutoFormatRecordDate(string rawText)
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

            if (TryBuildRecordDateString(currentYear, mmdd.Substring(0, 2), mmdd.Substring(2, 2), out string formattedDate))
            {
                return formattedDate;
            }
        }

        if (digits.Length >= 8)
        {
            string yyyy = digits.Substring(0, 4);
            string mm = digits.Substring(4, 2);
            string dd = digits.Substring(6, 2);

            if (TryBuildRecordDateString(yyyy, mm, dd, out string formattedDate))
            {
                return formattedDate;
            }
        }

        return rawText.Trim();
    }

    private string AutoFormatRecordTime(string rawText)
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

    private bool TryBuildRecordDateString(int year, string monthText, string dayText, out string formattedDate)
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

    private bool TryBuildRecordDateString(string yearText, string monthText, string dayText, out string formattedDate)
    {
        formattedDate = string.Empty;

        if (!int.TryParse(yearText, out int year))
        {
            return false;
        }

        return TryBuildRecordDateString(year, monthText, dayText, out formattedDate);
    }
}
