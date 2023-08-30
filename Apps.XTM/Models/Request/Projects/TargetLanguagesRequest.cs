using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Request.Projects;

public class TargetLanguagesRequest
{
    [Display("Target languages")]
    public IEnumerable<string> TargetLanguages { get; set; }
}