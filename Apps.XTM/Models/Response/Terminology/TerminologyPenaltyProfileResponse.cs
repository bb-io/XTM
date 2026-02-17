using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Terminology;

public class TerminologyPenaltyProfileResponse
{
    [Display("id")]
    public required string Id { get; set; }

    [Display("name")]
    public required string Name { get; set; }
}
