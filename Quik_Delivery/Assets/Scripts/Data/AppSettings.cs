using System;

[Serializable]
public class AppSettings
{
    public string LanguageCode = "ko";
    public bool UseLocationAutoFill = true;
    public bool UseLargeFontMode = true;
    public bool ConfirmBeforeDelete = true;

    public DeliveryType DefaultDeliveryType = DeliveryType.Quick;
    public string DefaultPricingRuleId = string.Empty;
}