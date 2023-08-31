using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.SubjectMatters;

public record ListSubjectMattersResponse(
    [property: Display("Subject matters")] List<SubjectMatterResponse> SubjectMatters);