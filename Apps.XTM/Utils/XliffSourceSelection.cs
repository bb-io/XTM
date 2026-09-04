using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Bilingual.Xliff2;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Apps.XTM.Utils;

public static partial class XliffSourceSelection
{
    private const string BlackbirdNamespace = "https://blackbird.io/xliff/xtm-source-selection";
    private const string ExcludedByBlackbirdAttribute = "exluded";

    public static PreparedSourceXliff Prepare(byte[] content, IEnumerable<string>? excludedStates)
    {
        Transformation transformation;
        bool sourceIsXliff2;

        try
        {
            using var stream = new MemoryStream(content);

            if (Xliff2Serializer.IsXliff2(stream, out var xliff2Node))
            {
                transformation = Xliff2Serializer.Deserialize(xliff2Node);
                sourceIsXliff2 = true;
            }
            else if (Xliff1Serializer.IsXliff1(stream, out var xliff1Node))
            {
                transformation = Xliff1Serializer.Deserialize(xliff1Node);
                sourceIsXliff2 = false;
            }
            else
            {
                throw new PluginMisconfigurationException(
                    "The source file must be a valid XLIFF 1 or XLIFF 2 file.");
            }
        }
        catch (PluginMisconfigurationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new PluginMisconfigurationException(
                $"The source file must be a valid XLIFF 1 or XLIFF 2 file. {exception.Message}");
        }

        var states = NormalizeStates(excludedStates);
        var units = transformation.GetUnits().ToArray();
        var total = 0;
        var excluded = 0;
        var approximateWordCount = 0;
        var hasBlackbirdExclusions = false;
        var markerName = XNamespace.Get(BlackbirdNamespace) + ExcludedByBlackbirdAttribute;

        foreach (var unit in units)
        {
            var segments = unit.Segments.Where(x => !sourceIsXliff2 || !x.IsIgnorbale).ToArray();
            if (segments.Length == 0)
                continue;

            if (sourceIsXliff2 && segments.Length > 1)
            {
                var unitId = unit.Id ?? "(missing ID)";
                throw new PluginMisconfigurationException(
                    $"XLIFF unit '{unitId}' contains multiple segments. " +
                    "This action currently supports one segment per unit so the XLIFF 2 to XLIFF 1.2 conversion remains lossless.");
            }

            var unitSegmentCount = sourceIsXliff2 ? segments.Length : 1;
            total += unitSegmentCount;
            var alreadyExcluded = unit.Translate == false;
            var selectedSegments = segments.Where(segment =>
            {
                var state = (segment.State ?? SegmentState.Initial).Serialize();
                return states.Contains(state)
                    || (!sourceIsXliff2
                        && segment.State == SegmentState.Reviewed
                        && states.Contains(SegmentState.Final.Serialize()));
            }).ToArray();

            if (!alreadyExcluded && selectedSegments.Length > 0 && selectedSegments.Length != segments.Length)
            {
                var unitId = unit.Id ?? "(missing ID)";
                throw new PluginMisconfigurationException(
                    $"XLIFF unit '{unitId}' contains both excluded and translatable segments. " +
                    "XLIFF translate='no' applies to the whole unit, so this file cannot be filtered safely.");
            }

            var excludeUnit = alreadyExcluded || selectedSegments.Length == segments.Length;
            if (excludeUnit)
            {
                if (!alreadyExcluded)
                {
                    unit.Other.RemoveAll(x => x is XAttribute attribute && attribute.Name == markerName);
                    unit.Other.Add(new XAttribute(markerName, "true"));
                    hasBlackbirdExclusions = true;
                }

                unit.Translate = false;
                excluded += unitSegmentCount;
            }
            else
            {
                approximateWordCount += segments.Sum(CountSourceWords);
            }
        }

        if (total == 0)
            throw new PluginMisconfigurationException("The XLIFF file does not contain any segments.");

        if (hasBlackbirdExclusions)
        {
            transformation.XliffOther.RemoveAll(x => x is XAttribute attribute
                && attribute.Name == XNamespace.Xmlns + "bb"
                && attribute.Value != BlackbirdNamespace);

            if (!transformation.XliffOther.OfType<XAttribute>().Any(x => x.Name == XNamespace.Xmlns + "bb"))
                transformation.XliffOther.Add(new XAttribute(XNamespace.Xmlns + "bb", BlackbirdNamespace));
        }

        var xliff1 = Xliff1Serializer.Serialize(transformation);
        return new PreparedSourceXliff(
            Encoding.UTF8.GetBytes(xliff1),
            total,
            excluded,
            total - excluded,
            approximateWordCount);
    }

    public static byte[] RemoveBlackbirdExclusions(byte[] content)
    {
        Transformation transformation;

        using (var stream = new MemoryStream(content))
        {
            if (Xliff1Serializer.IsXliff1(stream, out var xliff1Node))
                transformation = Xliff1Serializer.Deserialize(xliff1Node);
            else if (Xliff2Serializer.IsXliff2(stream, out var xliff2Node))
                transformation = Xliff2Serializer.Deserialize(xliff2Node);
            else
                return content;
        }

        var markerName = XNamespace.Get(BlackbirdNamespace) + ExcludedByBlackbirdAttribute;
        var restored = false;
        var units = transformation.GetUnits().ToArray();

        foreach (var unit in units)
        {
            var markers = unit.Other.OfType<XAttribute>()
                .Where(x => x.Name == markerName
                    && string.Equals(x.Value, "true", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (markers.Length == 0)
                continue;

            unit.Translate = null;
            unit.Other.RemoveAll(markers.Contains);
            restored = true;
        }

        if (!restored)
            return content;

        if (!units.SelectMany(x => x.Other.OfType<XAttribute>()).Any(x => x.Name == markerName))
        {
            transformation.XliffOther.RemoveAll(x => x is XAttribute attribute
                && attribute.Name == XNamespace.Xmlns + "bb"
                && attribute.Value == BlackbirdNamespace);
        }

        return Encoding.UTF8.GetBytes(Xliff1Serializer.Serialize(transformation));
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

    private static string NormalizeState(string? state) =>
        string.IsNullOrWhiteSpace(state) ? "initial" : state.Trim().ToLowerInvariant();

    private static int CountSourceWords(Segment segment) => WordRegex().Matches(
        string.Concat(segment.Source.Where(x => x is not InlineTag).Select(x => x.Value))).Count;

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['\u2019.-][\p{L}\p{N}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}

public record PreparedSourceXliff(
    byte[] Content,
    int SegmentsTotal,
    int SegmentsExcluded,
    int SegmentsLeft,
    int ApproximateWordCount);
