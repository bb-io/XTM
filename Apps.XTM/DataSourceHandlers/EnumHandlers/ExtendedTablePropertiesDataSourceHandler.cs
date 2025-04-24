using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers
{
    public class ExtendedTablePropertiesDataSourceHandler : EnumDataHandler
    {
        protected override Dictionary<string, string> EnumValues => new()
        {

            { "extendedReportType", "Single report for all project files"},
            { "includeSegmentId", "Segment ID" },
            { "includeSegmentKey", "Segment key" },
            { "includeSource", "Source" },
            { "includeTarget", "Target" },
            { "includeXTMStatus", "XTM status" },
            { "includeXliffDocStatus", "Xliff doc status" },
            { "includeComments", "Comments" },
            { "includeRevisions", "Revisions" },
            { "includeMatches", "Matches" },
            { "includeQaWarnings", "QA warnings" },
            { "includeOnlySegmentsWithQaWarnings", "Only segments with QA warnings" },
            { "includeLQAErrors", "LQA errors" },
            { "includePreTranslatedText", "Pre-translated text" },
            { "includePostEditedText", "Post-edited text" },
            { "includeFinalText", "Final text" },
            { "includeEditDistanceScore", "Edit distance score" },
            { "populateTargetWithSource","Populate target with source" }

        };
    }
}
