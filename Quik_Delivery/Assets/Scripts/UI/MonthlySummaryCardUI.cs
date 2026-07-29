using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation-only monthly summary card. It creates its text rows when used
/// from the runtime-built SettlementPanel hierarchy.
/// </summary>
public class MonthlySummaryCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text txtMonthTitle;
    [SerializeField] private TMP_Text txtDeliveryCount;
    [SerializeField] private TMP_Text txtGrossRevenue;
    [SerializeField] private TMP_Text txtCompanyDeposit;
    [SerializeField] private TMP_Text txtDataUsageFee;
    [SerializeField] private TMP_Text txtTollFee;
    [SerializeField] private TMP_Text txtElectricCharge;
    [SerializeField] private TMP_Text txtOtherExpense;
    [SerializeField] private TMP_Text txtTotalExpense;
    [SerializeField] private TMP_Text txtNetRevenue;

    public void Bind(int year, int month, DeliveryManager.DeliverySummary summary)
    {
        EnsureTextFields();

        if (summary == null)
        {
            summary = new DeliveryManager.DeliverySummary();
        }

        txtMonthTitle.text = $"{year}년 {month}월";
        txtDeliveryCount.text = $"배송 건수: {summary.TotalCount:N0}건";
        txtGrossRevenue.text = $"총수입: {summary.TotalGrossRevenue:N0}원";
        txtCompanyDeposit.text = $"회사입금 총액: {summary.TotalCommissionFee:N0}원";
        txtDataUsageFee.text = $"데이터사용료 총액: {summary.TotalDataUsageFee:N0}원";
        txtTollFee.text = $"통행료 총액: {summary.TotalTollFee:N0}원";
        txtElectricCharge.text = $"차량전기비 총액: {summary.TotalElectricCharge:N0}원";
        txtOtherExpense.text = $"기타 지출: {summary.TotalOtherDeduction:N0}원";
        txtTotalExpense.text = $"지출합계: {summary.TotalExpense:N0}원";
        txtNetRevenue.text = $"실수령액: {summary.TotalNetRevenue:N0}원";
    }

    private void EnsureTextFields()
    {
        if (txtMonthTitle != null)
        {
            return;
        }

        VerticalLayoutGroup layout = gameObject.GetComponent<VerticalLayoutGroup>();

        if (layout == null)
        {
            layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 22, 22);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        txtMonthTitle = CreateText("Txt_MonthTitle", 38f, FontStyles.Bold);
        txtDeliveryCount = CreateText("Txt_DeliveryCount", 27f, FontStyles.Normal);
        txtGrossRevenue = CreateText("Txt_GrossRevenue", 27f, FontStyles.Normal);
        txtCompanyDeposit = CreateText("Txt_CompanyDeposit", 27f, FontStyles.Normal);
        txtDataUsageFee = CreateText("Txt_DataUsageFee", 27f, FontStyles.Normal);
        txtTollFee = CreateText("Txt_TollFee", 27f, FontStyles.Normal);
        txtElectricCharge = CreateText("Txt_ElectricCharge", 27f, FontStyles.Normal);
        txtOtherExpense = CreateText("Txt_OtherExpense", 27f, FontStyles.Normal);
        txtTotalExpense = CreateText("Txt_TotalExpense", 29f, FontStyles.Bold);
        txtNetRevenue = CreateText("Txt_NetRevenue", 31f, FontStyles.Bold);
    }

    private TMP_Text CreateText(string objectName, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(transform, false);

        LayoutElement element = textObject.GetComponent<LayoutElement>();
        element.minHeight = 38f;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = new Color(0.12f, 0.11f, 0.09f, 1f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        return text;
    }
}
