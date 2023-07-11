using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.Projects;

public class XliffOptions
{
    [JsonProperty("autopopulation")]
    public string Autopopulation { get; set; }
    [JsonProperty("segmentStatusApproving")]
    public string SegmentStatusApproving { get; set; }
}