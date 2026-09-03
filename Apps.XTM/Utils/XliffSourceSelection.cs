using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Bilingual.Xliff2;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Apps.XTM.Utils;

public static partial class XliffSourceSelection
{
    public static PreparedSourceXliff Prepare(byte[] content, IEnumerable<string>? excludedStates)
    {
        XDocument document;
        try
        {
            using var stream = new MemoryStream(content);
            document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception)
        {
            throw new PluginMisconfigurationException($"The source file is not valid XML. {exception.Message}");
        }

        if (document.Root?.Name.LocalName != "xliff")
            throw new PluginMisconfigurationException("The source file must be a valid XLIFF 1 or XLIFF 2 file.");

        var states = NormalizeStates(excludedStates);
        var version = document.Root.Attribute("version")?.Value;

        return version?.StartsWith('2') == true
            ? PrepareXliff2(document, states)
            : version?.StartsWith('1') == true
                ? PrepareXliff1(document, states)
                : throw new PluginMisconfigurationException($"Unsupported XLIFF version '{version ?? "unknown"}'. Only XLIFF 1 and XLIFF 2 are supported.");
    }

    private static PreparedSourceXliff PrepareXliff2(XDocument document, HashSet<string> excludedStates)
    {
        var root = document.Root!;
        var units = root.Descendants().Where(x => x.Name.LocalName == "unit").ToArray();
        var excludedUnitIds = new HashSet<string>(StringComparer.Ordinal);
        var total = 0;
        var excluded = 0;
        var approximateWordCount = 0;

        foreach (var unit in units)
        {
            var segments = unit.Elements().Where(x => x.Name.LocalName == "segment").ToArray();
            if (segments.Length == 0)
                continue;

            if (segments.Length > 1)
            {
                var unitId = unit.Attribute("id")?.Value ?? "(missing ID)";
                throw new PluginMisconfigurationException(
                    $"XLIFF unit '{unitId}' contains multiple segments. " +
                    "This action currently supports one segment per unit so the XLIFF 2 to XLIFF 1.2 conversion remains lossless.");
            }

            total += segments.Length;
            var alreadyExcluded = IsTranslateNo(unit);
            var selectedSegments = segments.Where(segment => StateIsExcluded(segment.Attribute("state")?.Value, excludedStates)).ToArray();

            if (!alreadyExcluded && selectedSegments.Length > 0 && selectedSegments.Length != segments.Length)
            {
                var unitId = unit.Attribute("id")?.Value ?? "(missing ID)";
                throw new PluginMisconfigurationException(
                    $"XLIFF unit '{unitId}' contains both excluded and translatable segments. " +
                    "XLIFF translate='no' applies to the whole unit, so this file cannot be filtered safely.");
            }

            var excludeUnit = alreadyExcluded || selectedSegments.Length == segments.Length;
            if (excludeUnit)
            {
                unit.SetAttributeValue("translate", "no");
                excluded += segments.Length;

                var unitId = unit.Attribute("id")?.Value;
                if (string.IsNullOrWhiteSpace(unitId))
                    throw new PluginMisconfigurationException("Every excluded XLIFF unit must have an ID.");

                excludedUnitIds.Add(unitId);
            }
            else
            {
                approximateWordCount += segments.Sum(CountSourceWords);
            }
        }

        if (total == 0)
            throw new PluginMisconfigurationException("The XLIFF file does not contain any segments.");

        var transformation = Xliff2Serializer.Deserialize(root);
        var xliff1 = Xliff1Serializer.Serialize(transformation);
        var converted = XDocument.Parse(xliff1, LoadOptions.PreserveWhitespace);

        var matchedExcludedUnits = 0;
        foreach (var transUnit in converted.Descendants().Where(x => x.Name.LocalName == "trans-unit"))
        {
            var id = transUnit.Attribute("id")?.Value;
            if (id != null && excludedUnitIds.Contains(id))
            {
                transUnit.SetAttributeValue("translate", "no");
                matchedExcludedUnits++;
            }
        }

        if (matchedExcludedUnits != excludedUnitIds.Count)
        {
            throw new PluginMisconfigurationException(
                "Some excluded XLIFF units could not be mapped during XLIFF 2 to XLIFF 1.2 conversion.");
        }

        return CreateResult(converted, total, excluded, approximateWordCount);
    }

    private static PreparedSourceXliff PrepareXliff1(XDocument document, HashSet<string> excludedStates)
    {
        var transUnits = document.Root!.Descendants().Where(x => x.Name.LocalName == "trans-unit").ToArray();
        if (transUnits.Length == 0)
            throw new PluginMisconfigurationException("The XLIFF file does not contain any segments.");

        var excluded = 0;
        var approximateWordCount = 0;

        foreach (var transUnit in transUnits)
        {
            var targetState = transUnit.Elements().FirstOrDefault(x => x.Name.LocalName == "target")?.Attribute("state")?.Value;
            if (IsTranslateNo(transUnit) || StateIsExcluded(targetState, excludedStates))
            {
                transUnit.SetAttributeValue("translate", "no");
                excluded++;
            }
            else
            {
                approximateWordCount += CountSourceWords(transUnit);
            }
        }

        return CreateResult(document, transUnits.Length, excluded, approximateWordCount);
    }

    private static PreparedSourceXliff CreateResult(XDocument document, int total, int excluded, int approximateWordCount)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true))
            document.Save(writer, SaveOptions.DisableFormatting);

        return new PreparedSourceXliff(stream.ToArray(), total, excluded, total - excluded, approximateWordCount);
    }

    private static HashSet<string> NormalizeStates(IEnumerable<string>? states)
    {
        var normalized = states?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeState)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        if (normalized.Count == 0)
            normalized.Add("final");

        return normalized;
    }

    private static bool StateIsExcluded(string? state, HashSet<string> excludedStates)
    {
        var normalized = NormalizeState(state);
        if (excludedStates.Contains(normalized))
            return true;

        // XLIFF 1.2 commonly represents a final/approved segment as signed-off.
        return normalized == "signed-off" && excludedStates.Contains("final");
    }

    private static string NormalizeState(string? state) =>
        string.IsNullOrWhiteSpace(state) ? "initial" : state.Trim().ToLowerInvariant();

    private static bool IsTranslateNo(XElement element) =>
        string.Equals(element.Attribute("translate")?.Value, "no", StringComparison.OrdinalIgnoreCase);

    private static int CountSourceWords(XElement segmentOrTransUnit)
    {
        var source = segmentOrTransUnit.Elements().FirstOrDefault(x => x.Name.LocalName == "source");
        return source == null ? 0 : WordRegex().Matches(source.Value).Count;
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['\u2019.-][\p{L}\p{N}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}

public record PreparedSourceXliff(
    byte[] Content,
    int SegmentsTotal,
    int SegmentsExcluded,
    int SegmentsLeft,
    int ApproximateWordCount);
