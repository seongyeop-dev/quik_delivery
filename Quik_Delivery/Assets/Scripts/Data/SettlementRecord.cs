using System;

[Serializable]
public class SettlementRecord
{
    public string Id = Guid.NewGuid().ToString();

    public string StartDate = string.Empty; // yyyy-MM-dd
    public string EndDate = string.Empty;   // yyyy-MM-dd

    public int TotalGrossRevenue = 0;
    public int TotalDeduction = 0;
    public int TotalNetRevenue = 0;

    public int SettledAmount = 0;
    public int UnsettledAmount = 0;

    public SettlementStatus Status = SettlementStatus.Unsettled;
    public string Memo = string.Empty;
}