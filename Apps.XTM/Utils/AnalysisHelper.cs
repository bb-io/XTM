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
            string originalLocale = langNode["targetLanguage"]!.ToString();
            string normalizedLocale = langNode["targetLanguage"]!.ToString().Replace('_', '-');
            JObject coreMetrics = (langNode["coreMetrics"] as JObject)!;

            AddAnalysis(results, coreMetrics, normalizedLocale, originalLocale);
        }

        return results;
    }
        
    private static void AddAnalysis(
        List<Analysis> analysis, 
        JObject metrics, 
        string normalizedLocale,
        string originalLocale)
    {
        var rawDict = new Dictionary<string, decimal>();
        foreach (JProperty property in metrics.Properties())
            rawDict[property.Name] = property.Value.Value<decimal>();

        var analysisRecord = Analysis.Map(normalizedLocale, originalLocale, rawDict, AnalysisMap);
        analysis.Add(analysisRecord);
    }
}