using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Webhooks.Models.Payload;

public class ProjectFinishedPayload
{
    [Display("Project ID")] public string ProjectId { get; set; }

    [Display("Customer ID")] public string CustomerId { get; set; }

    [Display("UUID")] public string Uuid { get; set; }
}