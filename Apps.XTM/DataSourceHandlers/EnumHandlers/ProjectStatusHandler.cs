using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class ProjectStatusHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new()
        {
            ["NOT_STARTED"] = "Not started",
            ["STARTED"] = "Started",
            ["FINISHED"] = "Finished"
        };
    }
}