using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class DueDateTypeDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new("projectDueDate", "Project due date"),
        new("newSourceDueDate", "Preprocessing due date"),
        new("workflowStartDate", "Workflow start date"),
        new("workflowDueDate", "Workflow due date"),
        new("steps", "Step due date"),
        new("jobs", "Job due date"),
        new("targetLanguages", "Target language due date"),
    ];
}
