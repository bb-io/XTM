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
    private const string ExcludedByBlackbirdAttribute = "excluded";

    public static PreparedSourceXliff Prepare(byte[] content, IEnumerable<string>? excludedStates,
        string fileName = "source.xlf", string? contentType = null, string? sourceLanguage = null)
    {
        Transformation transformation;
        bool sourceIsXliff1;

        try
        {
            if (content.AsSpan().StartsWith(Encoding.UTF8.Preamble))
                content = content[Encoding.UTF8.Preamble.Length..];
            using var stream = new MemoryStream(content);

            sourceIsXliff1 = Xliff1Serializer.IsXliff1(stream, out _);
            stream.Position = 0;
            var loaded = Transformation.Load(stream, fileName, contentType);
            if (!loaded.Success || loaded.Value is null)
                throw new PluginMisconfigurationException(
                    $"The source file could not be read. Provide a file supported by Blackbird filters. {loaded.Error}");

            transformation = loaded.Value;
            if (string.IsNullOrWhiteSpace(transformation.SourceLanguage))
                transformation.SourceLanguage = sourceLanguage?.Replace('_', '-');
        }
        catch (PluginMisconfigurationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new PluginMisconfigurationException(
                $"The source file could not be read. Provide a file supported by Blackbird filters. {exception.Message}");
        }

        var states = NormalizeStates(excludedStates);
        var nodes = new Stack<(Node Node, bool Translate)>();
        nodes.Push((transformation, true));
        var total = 0;
        var excluded = 0;
        var approximateWordCount = 0;
        var hasBlackbirdExclusions = false;
        var markerName = XNamespace.Get(BlackbirdNamespace) + ExcludedByBlackbirdAttribute;

        while (nodes.TryPop(out var item))
        {
            var translate = item.Node.Translate ?? item.Translate;
            if (item.Node is Transformation container)
            {
                foreach (var child in container.Children)
                    nodes.Push((child, translate));
            }
            else if (item.Node is Blackbird.Filters.Transformations.Group group)
            {
                foreach (var child in group.Children)
                    nodes.Push((child, translate));
            }

            if (item.Node is not Unit unit)
                continue;

            var segments = unit.Segments.Where(x => sourceIsXliff1 || !x.IsIgnorbale).ToArray();
            if (segments.Length == 0)
                continue;

            var unitSegmentCount = segments.Length;
            total += unitSegmentCount;
            var alreadyExcluded = !translate;
            var selectedSegments = segments.Where(segment =>
            {
                var state = (segment.State ?? SegmentState.Initial).Serialize();
                return states.Contains(state)
                    || (sourceIsXliff1
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
                    if (unit.Translate == true)
                        unit.Other.Add(new XAttribute(XNamespace.Get(BlackbirdNamespace) + "original-translate", "yes"));
                    unit.Translate = false;
                    hasBlackbirdExclusions = true;
                }

                excluded += unitSegmentCount;
            }
            else
            {
                approximateWordCount += segments.Sum(CountSourceWords);
            }
        }

        if (total == 0)
            throw new PluginMisconfigurationException("The source file does not contain any segments to translate.");

        if (hasBlackbirdExclusions)
        {
            var namespaceAttributes = transformation.XliffOther.OfType<XAttribute>().ToArray();
            if (!namespaceAttributes.Any(x => x.IsNamespaceDeclaration && x.Value == BlackbirdNamespace))
            {
                var prefix = "bb";
                while (namespaceAttributes.Any(x => x.Name == XNamespace.Xmlns + prefix))
                    prefix += "x";
                transformation.XliffOther.Add(new XAttribute(XNamespace.Xmlns + prefix, BlackbirdNamespace));
            }
        }

        var xliff = Xliff2Serializer.Serialize(transformation, Xliff2Version.Xliff21);

        return new PreparedSourceXliff(
            Encoding.UTF8.GetBytes(xliff),
            total,
            excluded,
            total - excluded,
            approximateWordCount);
    }

    public static byte[] RemoveBlackbirdExclusions(byte[] content)
    {
        XDocument document;
        try
        {
            using var stream = new MemoryStream(content);
            document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
        catch (System.Xml.XmlException)
        {
            return content;
        }

        if (document.Root?.Name.LocalName != "xliff"
            || document.Root.Name.NamespaceName is not ("urn:oasis:names:tc:xliff:document:1.2"
                or "urn:oasis:names:tc:xliff:document:2.0" or "urn:oasis:names:tc:xliff:document:2.2"))
            return content;

        var markerName = XNamespace.Get(BlackbirdNamespace) + ExcludedByBlackbirdAttribute;
        var restored = false;

        foreach (var unit in document.Descendants().Where(x => x.Name.Namespace == document.Root.Name.Namespace
            && x.Name.LocalName is "unit" or "trans-unit"))
        {
            var marker = unit.Attribute(markerName);
            if (!string.Equals(marker?.Value, "true", StringComparison.OrdinalIgnoreCase))
                continue;

            if (unit.Attribute("translate")?.Value == "no")
            {
                if (unit.Attribute(XNamespace.Get(BlackbirdNamespace) + "original-translate")?.Value == "yes")
                    unit.SetAttributeValue("translate", "yes");
                else
                    unit.Attribute("translate")!.Remove();
            }
            unit.Attribute(XNamespace.Get(BlackbirdNamespace) + "original-translate")?.Remove();
            marker!.Remove();
            restored = true;
        }

        if (!restored)
            return content;

        if (!document.Descendants().Any(x => x.Name.NamespaceName == BlackbirdNamespace
            || x.Attributes().Any(a => !a.IsNamespaceDeclaration && a.Name.NamespaceName == BlackbirdNamespace)))
        {
            document.Descendants().Attributes().Where(x => x.IsNamespaceDeclaration
                && x.Value == BlackbirdNamespace).Remove();
        }

        using var output = new MemoryStream();
        using (var writer = System.Xml.XmlWriter.Create(output, new System.Xml.XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = document.Declaration is null,
        }))
            document.Save(writer);
        return output.ToArray();
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
