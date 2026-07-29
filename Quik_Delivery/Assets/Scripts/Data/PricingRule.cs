using System;

[Serializable]
public class PricingRule
{
    public string Id = Guid.NewGuid().ToString();
    public string RuleName = "기본 단가";

    public int DefaultBaseFee = 0;
    public int DefaultExtraFee = 0;
    public int DefaultSurgeFee = 0;
    public int DefaultWaitingFee = 0;
    public int DefaultCommissionFee = 0;
    public int DefaultOtherDeduction = 0;
}