using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class PaymentStatusDataHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => new()
    {
        { "NOT_REQUIRED", "Not required" },
        { "PAID", "Paid" },
        { "WAITING_FOR_PAYMENT", "Waiting for payment" }
    };
}