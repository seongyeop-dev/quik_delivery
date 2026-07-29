using UnityEngine;

public class RevenueCalculator : MonoBehaviour
{
    /// <summary>
    /// 기존 호환용 총수입 계산
    /// 총수입 = 기본요금 + 추가요금
    /// </summary>
    public int CalculateGrossRevenue(int baseFee, int extraFee)
    {
        return CalculateGrossRevenue(baseFee, 0, extraFee);
    }

    /// <summary>
    /// 총수입 계산
    /// 총수입 = 기본요금 + 기본요금+부가세 입력값 + 추가요금
    /// </summary>
    public int CalculateGrossRevenue(int baseFee, int baseFeeVatSource, int extraFee)
    {
        int safeBaseFee = SanitizeAmount(baseFee);
        int safeBaseFeeVatSource = SanitizeAmount(baseFeeVatSource);
        int safeExtraFee = SanitizeAmount(extraFee);

        return safeBaseFee + safeBaseFeeVatSource + safeExtraFee;
    }

    /// <summary>
    /// 부가세 계산
    /// 부가세 = 기본요금+부가세 입력값의 10%
    /// 부가세는 별도 표시/조회용이며 총비용이나 실수령액에서 직접 차감하지 않는다.
    /// </summary>
    public int CalculateVatAmount(int baseFeeVatSource)
    {
        int safeBaseFeeVatSource = SanitizeAmount(baseFeeVatSource);
        return Mathf.RoundToInt(safeBaseFeeVatSource * 0.1f);
    }

    /// <summary>
    /// 기존 4개 비용 항목 호환용 계산
    /// 총비용 = 회사입금 + 차감액 + 차량전기비 + 통행료
    /// </summary>
    public int CalculateTotalExpense(
        int commissionFee,
        int otherDeduction,
        int electricCharge,
        int tollFee)
    {
        return CalculateTotalExpense(
            commissionFee,
            0,
            otherDeduction,
            electricCharge,
            tollFee
        );
    }

    /// <summary>
    /// 총비용 계산
    /// 총비용 = 회사입금 + 데이터사용료 + 차감액 + 차량전기비 + 통행료
    /// </summary>
    public int CalculateTotalExpense(
        int commissionFee,
        int dataUsageFee,
        int otherDeduction,
        int electricCharge,
        int tollFee)
    {
        int safeCommissionFee = SanitizeAmount(commissionFee);
        int safeDataUsageFee = SanitizeAmount(dataUsageFee);
        int safeOtherDeduction = SanitizeAmount(otherDeduction);
        int safeElectricCharge = SanitizeAmount(electricCharge);
        int safeTollFee = SanitizeAmount(tollFee);

        return safeCommissionFee + safeDataUsageFee + safeOtherDeduction + safeElectricCharge + safeTollFee;
    }

    /// <summary>
    /// 실수령액 계산
    /// 실수령액 = 총수입 - 총비용
    /// </summary>
    public int CalculateNetRevenue(int grossRevenue, int totalExpense)
    {
        int safeGrossRevenue = SanitizeAmount(grossRevenue);
        int safeTotalExpense = SanitizeAmount(totalExpense);

        return safeGrossRevenue - safeTotalExpense;
    }

    /// <summary>
    /// DeliveryRecord에 계산 결과 반영
    /// </summary>
    public void ApplyCalculation(DeliveryRecord record)
    {
        if (record == null)
        {
            Debug.LogWarning("[RevenueCalculator] DeliveryRecord가 null입니다.");
            return;
        }

        // 수입 항목
        record.BaseFee = SanitizeAmount(record.BaseFee);
        record.BaseFeeVatSource = SanitizeAmount(record.BaseFeeVatSource);
        record.VatAmount = CalculateVatAmount(record.BaseFeeVatSource);
        record.ExtraFee = SanitizeAmount(record.ExtraFee);

        // 비용 항목
        record.CommissionFee = SanitizeAmount(record.CommissionFee);
        record.DataUsageFee = SanitizeAmount(record.DataUsageFee);
        record.OtherDeduction = SanitizeAmount(record.OtherDeduction);
        record.ElectricCharge = SanitizeAmount(record.ElectricCharge);
        record.TollFee = SanitizeAmount(record.TollFee);

        record.GrossRevenue = CalculateGrossRevenue(
            record.BaseFee,
            record.BaseFeeVatSource,
            record.ExtraFee
        );

        record.TotalExpense = CalculateTotalExpense(
            record.CommissionFee,
            record.DataUsageFee,
            record.OtherDeduction,
            record.ElectricCharge,
            record.TollFee
        );

        record.NetRevenue = CalculateNetRevenue(
            record.GrossRevenue,
            record.TotalExpense
        );
    }

    /// <summary>
    /// 음수 입력 방지
    /// </summary>
    private int SanitizeAmount(int amount)
    {
        return Mathf.Max(0, amount);
    }
}
