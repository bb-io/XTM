﻿using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response.Projects;

public class ProjectCompletionResponse
{
    public string Activity { get; set; }
    public List<JobResponse> Jobs { get; set; }

    [Display("Job IDs")]
    public IEnumerable<string> JobIds { get; set; }

    public ProjectCompletionResponse()
    {
        Activity = string.Empty;
        Jobs = new();
    }

    public ProjectCompletionResponse(checkProjectCompletionResponse response)
    {
        Activity = response?.@return?.project?.activity.ToString() ?? string.Empty;
        Jobs = new List<JobResponse>();

        var jobList = response?.@return?.project?.jobs ?? Enumerable.Empty<xtmJobStatusResponseAPI>();

        JobIds = jobList
               .Select(x => x.jobDescriptor?.id.ToString() ?? string.Empty)
               .ToList();

        foreach (var job in jobList)
        {
            var jobResponse = new JobResponse
            {
                JobId = job.jobDescriptor.id.ToString() ?? string.Empty,
                FileName = job.fileName,
                SourceFileId = job.sourceFileId.ToString() ?? "",
                TargetLanguage = job.targetLanguage.ToString() ?? "",
                JoinFilesType = job.joinFilesType.ToString() ?? "",
                Steps = new List<StepResponse>()
            };

            foreach (var step in job.steps ?? Array.Empty<xtmStepStatusResponseAPI>())
            {
                var stepResponse = new StepResponse
                {
                    Status = step.status.ToString(),
                    StepName = step.stepDescriptor.workflowStepName,
                    DueToDate = step.dueDate
                };
                jobResponse.Steps.Add(stepResponse);
            }

            Jobs.Add(jobResponse);
        }
    }
}
