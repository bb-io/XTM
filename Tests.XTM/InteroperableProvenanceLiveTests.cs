using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Filters.Transformations;
using Tests.XTM.Base;

namespace Tests.XTM;

[TestClass]
public class InteroperableProvenanceLiveTests : TestBaseMultipleConnections
{
    [ContextDataSource(ConnectionTypes.Credentials), TestMethod, TestCategory("Live"), Timeout(360000)]
    public async Task Download_AcceptedUnchangedSegments_AttributesAssignedReviewer(InvocationContext context)
    {
        var projectId = Environment.GetEnvironmentVariable("XTM_PROVENANCE_PROJECT_ID");
        var jobId = Environment.GetEnvironmentVariable("XTM_PROVENANCE_JOB_ID");
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(jobId))
            Assert.Inconclusive("Set XTM_PROVENANCE_PROJECT_ID and XTM_PROVENANCE_JOB_ID to an isolated, assigned review/correct job with all segments signed off.");

        var action = new InteroperableActions(context, FileManager);
        foreach (var mode in new[] { SegmentAttributionDataSourceHandler.All,
                     SegmentAttributionDataSourceHandler.OnlyConfirmed, SegmentAttributionDataSourceHandler.None })
        {
            var response = await action.DownloadTranslatedInteroperableFile(
                new ProjectRequest { ProjectId = projectId },
                new DownloadTranslatedInteroperableFileRequest
                {
                    JobId = jobId,
                    AttributeSegmentsToUser = mode,
                    ProvenanceType = mode == SegmentAttributionDataSourceHandler.None ? "review" : null,
                });
            var projectDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
            var outputPath = Path.Combine(projectDirectory, "TestFiles", "Output", response.File.Name);
            try
            {
                using var stream = File.OpenRead(outputPath);
                var loaded = Transformation.Load(stream, response.File.Name, response.File.ContentType);
                Assert.IsTrue(loaded.Success, loaded.Error);
                var units = loaded.Value!.GetUnits().ToArray();
                Assert.HasCount(5, units, "The live probe contains five units, including one with two segments.");
                Assert.IsTrue(units.All(x => x.Provenance.Review.Tool?.StartsWith("XTM", StringComparison.Ordinal) == true));
                if (mode == SegmentAttributionDataSourceHandler.None)
                    Assert.IsTrue(units.All(x => x.Provenance.Review.Person is null));
                else
                    Assert.IsTrue(units.All(x => !string.IsNullOrWhiteSpace(x.Provenance.Review.Person)),
                        "Accepted unchanged translations must receive the selected reviewer assignment.");
                TestContext.WriteLine($"Verified {mode} review attribution in project {projectId}, job {jobId}.");
            }
            finally
            {
                File.Delete(outputPath);
            }
        }
    }
}
