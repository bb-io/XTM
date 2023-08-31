using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Request.SubjectMatters;

public class ListSubjectMattersRequest
{
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("activity")]
    [DataSource(typeof(ActivityDataHandler))]
    public string? Activity { get; set; }
}