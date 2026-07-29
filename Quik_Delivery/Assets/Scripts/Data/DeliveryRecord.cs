using System;

[Serializable]
public class DeliveryRecord
{
    public string Id = Guid.NewGuid().ToString();
    public string WorkSessionId = string.Empty;

    public string Date = string.Empty;       // yyyy-MM-dd
    public string StartTime = string.Empty;  // HH:mm
    public string EndTime = string.Empty;    // HH:mm

    public string StartAddress = string.Empty;
    public string EndAddress = string.Empty;

    public double StartLatitude = 0.0;
    public double StartLongitude = 0.0;
    public double EndLatitude = 0.0;
    public double EndLongitude = 0.0;

    // 현재 단계에서는 유지
    // 나중에 배송 종류 기능을 완전히 빼고 싶으면 마지막 정리 단계에서 제거
    public DeliveryType DeliveryType = DeliveryType.Quick;

    // 수입 항목
    public int BaseFee = 0;

    // 기본요금 + 부가세 입력값
    // 예: 10,000원을 입력하면 총수입에는 10,000원이 포함된다.
    public int BaseFeeVatSource = 0;

    // 기본요금 + 부가세 입력값에서 자동 계산된 부가세
    // 예: BaseFeeVatSource 10,000원 -> VatAmount 1,000원
    // 총수입/총비용/실수령액 계산에는 직접 영향 없이 별도 표시/조회용으로 사용한다.
    public int VatAmount = 0;

    public int ExtraFee = 0;

    // 비용 항목
    // CommissionFee는 현재 앱에서는 회사입금 항목으로 사용한다.
    public int CommissionFee = 0;

    // 하루 1회 자동 생성되는 데이터 사용료
    // 기존 JSON에는 이 필드가 없으므로 기존 기록은 0원으로 유지된다.
    public int DataUsageFee = 0;

    // 기타 차감액
    public int OtherDeduction = 0;
    public int ElectricCharge = 0;
    public int TollFee = 0;

    // 일일 고정비 자동 생성 기록 여부
    // true이면 배송 건수에는 포함하지 않고, 비용 합계에만 포함한다.
    public bool IsDailyFixedExpense = false;

    // 계산 결과
    // 총수입 = 기본요금 + 기본요금+부가세 입력값 + 추가요금
    public int GrossRevenue = 0;

    // 총비용 = 회사입금 + 데이터사용료 + 차감액 + 전기료 + 통행료
    public int TotalExpense = 0;

    // 실수령액 = 총수입 - 총비용
    // 부가세는 별도 표시/조회용이므로 실수령액에서 차감하지 않는다.
    public int NetRevenue = 0;

    public string Memo = string.Empty;

    public bool IsSettled = false;
    public string SettlementId = string.Empty;
}