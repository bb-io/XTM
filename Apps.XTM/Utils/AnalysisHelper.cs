using Blackbird.Filters.Analysis.Models;
using Newtonsoft.Json.Linq;

namespace Apps.XTM.Utils;

public static class AnalysisHelper
{
    private static readonly Dictionary<string, AnalysisType> AnalysisMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "iceMatchWords", AnalysisType.ExactMatch },
        { "leveragedWords", AnalysisType.ExactMatch },
        { "repeatsWords", AnalysisType.Repetition },
        { "highFuzzyMatchWords", AnalysisType.Match8594 },
        { "mediumFuzzyMatchWords", AnalysisType.Match7584 },
        { "lowFuzzyMatchWords", AnalysisType.Match5074 },
        { "noMatchWords", AnalysisType.NoMatch }
    };

    public static List<Analysis> GenerateAnalysis(JArray analysisContent)
    {
        List<Analysis> results = [];
        
        foreach (JToken langNode in analysisContent)
        {
            string locale = langNode["targetLanguage"]!.ToString();
            JObject coreMetrics = (langNode["coreMetrics"] as JObject)!;

            AddAnalysis(results, coreMetrics, locale);
        }

        return results;
    }
        
    private static void AddAnalysis(
        List<Analysis> analysis, 
        JObject metrics, 
        string locale)
    {
        var rawDict = new Dictionary<string, decimal>();
        foreach (JProperty property in metrics.Properties())
            rawDict[property.Name] = property.Value.Value<decimal>();

        var analysisRecord = Analysis.Map(locale, rawDict, AnalysisMap);
        analysis.Add(analysisRecord);
    }
}