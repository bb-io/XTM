using Apps.XTM.Utils;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Transformations;
using System.Text;
using System.Xml.Linq;

namespace Tests.XTM;

[TestClass]
public class XliffSourceSelectionTests
{
    [TestMethod]
    public void Prepare_HtmlWithBlackbirdMetadata_ProducesReloadableXliff21()
    {
        var input = """
            <!DOCTYPE html>
            <html lang="en-US">
              <head>
                <meta name="blackbird-ucid" content="article-123" />
                <meta name="blackbird-content-name" content="Travel guide" />
                <meta name="blackbird-system-name" content="Example CMS" />
                <meta name="blackbird-system-ref" content="https://cms.example.com" />
                <meta name="description" content="Page description" />
              </head>
              <body>
                <p data-blackbird-key="title">Travel guide</p>
                <p data-blackbird-key="description">Explore new places.</p>
              </body>
            </html>
            """;

        var result = XliffSourceSelection.Prepare([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(input)],
            null, "article.html", "text/html");
        var document = XDocument.Parse(Encoding.UTF8.GetString(result.Content));
        using var stream = new MemoryStream(result.Content);
        var loaded = Transformation.Load(stream, "article.xlf", "application/xliff+xml");

        Assert.AreEqual("2.1", document.Root?.Attribute("version")?.Value);
        Assert.AreEqual("urn:oasis:names:tc:xliff:document:2.0", document.Root?.Name.NamespaceName);
        Assert.AreEqual(0, result.SegmentsExcluded);
        Assert.AreEqual(3, result.SegmentsLeft); // Description metadata and both paragraphs are translatable.
        Assert.DoesNotContain("\uFEFF", Encoding.UTF8.GetString(result.Content));
        Assert.IsTrue(loaded.Success, loaded.Error);
        Assert.IsNotNull(loaded.Value);
        Assert.AreEqual("article-123", loaded.Value.SourceSystemReference?.ContentId);
        Assert.AreEqual("Travel guide", loaded.Value.SourceSystemReference?.ContentName);
        Assert.AreEqual("Example CMS", loaded.Value.SourceSystemReference?.SystemName);
        var source = loaded.Value.Source();
        Assert.IsTrue(source.Success, source.Error);
        Assert.AreEqual("article-123", source.Value!.SystemReference.ContentId);
        using var reader = new StreamReader(source.Value.ToStream());
        var reconstructedHtml = reader.ReadToEnd();
        StringAssert.Contains(reconstructedHtml, "data-blackbird-key=\"description\"");
        StringAssert.Contains(reconstructedHtml, "Explore new places.");
        StringAssert.Contains(reconstructedHtml, "name=\"description\" content=\"Page description\"");

        var target = loaded.Value.Target();
        Assert.IsTrue(target.Success, target.Error);
        using var targetReader = new StreamReader(target.Value!.ToStream());
        StringAssert.Contains(targetReader.ReadToEnd(), "name=\"description\" content=\"Page description\"");
    }

    [DataTestMethod]
    [DataRow("greeting.txt", "text/plain", "Hello from Blackbird.")]
    [DataRow("messages.json", "application/json", "{\"greeting\":\"Hello from Blackbird.\"}")]
    public void Prepare_SupportedMonolingualFile_ConvertsToXliff21(
        string fileName, string contentType, string input)
    {
        var result = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), null,
            fileName, contentType, "en-US");
        var document = XDocument.Parse(Encoding.UTF8.GetString(result.Content));
        using var stream = new MemoryStream(result.Content);
        var loaded = Transformation.Load(stream, "converted.xlf", "application/xliff+xml");

        Assert.AreEqual("2.1", document.Root?.Attribute("version")?.Value);
        Assert.AreEqual("en-US", document.Root?.Attribute("srcLang")?.Value);
        Assert.AreEqual(0, result.SegmentsExcluded);
        Assert.IsTrue(result.SegmentsLeft > 0);
        Assert.IsTrue(loaded.Success, loaded.Error);
        var source = loaded.Value!.Source();
        Assert.IsTrue(source.Success, source.Error);
        using var reader = new StreamReader(source.Value!.ToStream());
        StringAssert.Contains(reader.ReadToEnd(), "Hello from Blackbird.");
    }

    [TestMethod]
    public void Prepare_FileNotes_ArePreservedOnceInXliff21()
    {
        var input = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en">
              <file id="f1">
                <notes><note id="n1">Keep the product name unchanged.</note></notes>
                <unit id="u1"><segment><source>New text</source></segment></unit>
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), null);
        var notes = XDocument.Parse(Encoding.UTF8.GetString(result.Content))
            .Descendants(XNamespace.Get("urn:oasis:names:tc:xliff:document:2.0") + "note").ToArray();

        Assert.HasCount(1, notes);
        Assert.AreEqual("n1", notes[0].Attribute("id")?.Value);
        Assert.AreEqual("Keep the product name unchanged.", notes[0].Value);
    }

    [TestMethod]
    public void Prepare_TenSegmentTestFile_ReturnsExpectedStatistics()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestFiles", "Input", "selected-segments-10.xliff");

        var result = XliffSourceSelection.Prepare(File.ReadAllBytes(path), ["final"]);

        Assert.AreEqual(10, result.SegmentsTotal);
        Assert.AreEqual(7, result.SegmentsExcluded);
        Assert.AreEqual(3, result.SegmentsLeft);
        Assert.AreEqual(15, result.ApproximateWordCount);
    }

    [TestMethod]
    public void Prepare_FinalSegments_MarksUnitsAsNotTranslatableAndKeepsWholeFile()
    {
        var input = """
            <xliff srcLang="en-US" trgLang="fr-CA" version="2.1" xmlns="urn:oasis:names:tc:xliff:document:2.0">
              <file id="f1">
                <unit id="u1"><segment state="final"><source>Already translated text</source><target>Texte traduit</target></segment></unit>
                <unit id="u2"><segment><source>New text to translate</source><target /></segment></unit>
                <unit id="u3"><segment state="final"><source>Another completed string</source><target>Une autre chaîne</target></segment></unit>
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]);
        var document = XDocument.Parse(Encoding.UTF8.GetString(result.Content));
        var units = document.Descendants().Where(x => x.Name.LocalName == "unit").ToDictionary(
            x => x.Attribute("id")!.Value);

        Assert.AreEqual("2.1", document.Root?.Attribute("version")?.Value);
        Assert.AreEqual("urn:oasis:names:tc:xliff:document:2.0", document.Root?.Name.NamespaceName);
        Assert.AreEqual(3, result.SegmentsTotal);
        Assert.AreEqual(2, result.SegmentsExcluded);
        Assert.AreEqual(1, result.SegmentsLeft);
        Assert.AreEqual(4, result.ApproximateWordCount);
        Assert.AreEqual(3, units.Count);
        Assert.AreEqual("no", units["u1"].Attribute("translate")?.Value);
        Assert.AreEqual("true", units["u1"].Attributes().Single(x => x.Name.LocalName == "excluded").Value);
        Assert.IsNull(units["u2"].Attribute("translate"));
        Assert.AreEqual("no", units["u3"].Attribute("translate")?.Value);
        Assert.AreEqual("true", units["u3"].Attributes().Single(x => x.Name.LocalName == "excluded").Value);
    }

    [TestMethod]
    public void Prepare_AllSegmentsExcluded_ReturnsZeroSegmentsLeft()
    {
        var input = """
            <xliff version="1.2" source-language="en-US" target-language="fr-CA" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file original="messages.json" datatype="plaintext">
                <body>
                  <trans-unit id="1"><source>Completed text</source><target state="signed-off">Texte terminé</target></trans-unit>
                </body>
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]);

        Assert.AreEqual(1, result.SegmentsTotal);
        Assert.AreEqual(1, result.SegmentsExcluded);
        Assert.AreEqual(0, result.SegmentsLeft);
        Assert.AreEqual(0, result.ApproximateWordCount);
    }

    [TestMethod]
    public void Prepare_ExistingTranslateNo_PreservesAttributeThroughFiltersRoundTrip()
    {
        var input = """
            <xliff version="1.2" source-language="en-US" target-language="fr-CA" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file original="messages.json" datatype="plaintext">
                <body>
                  <trans-unit id="1" translate="no"><source>Originally excluded text</source><target>Texte exclu</target></trans-unit>
                  <trans-unit id="2"><source>Text to translate</source><target /></trans-unit>
                </body>
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]);
        var document = XDocument.Parse(Encoding.UTF8.GetString(result.Content));
        var units = document.Descendants().Where(x => x.Name.LocalName == "unit").ToDictionary(
            x => x.Attribute("id")!.Value);

        Assert.AreEqual("2.1", document.Root?.Attribute("version")?.Value);
        Assert.AreEqual("no", units["1"].Attribute("translate")?.Value);
        Assert.IsFalse(units["1"].Attributes().Any(x => x.Name.LocalName == "excluded"));
        Assert.IsNull(units["2"].Attribute("translate"));
        Assert.AreEqual(2, result.SegmentsTotal);
        Assert.AreEqual(1, result.SegmentsExcluded);
        Assert.AreEqual(1, result.SegmentsLeft);
    }

    [TestMethod]
    public void RemoveBlackbirdExclusions_RemovesOnlyMarkedTranslateNo()
    {
        var input = """
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:bb="https://blackbird.io/xliff/xtm-source-selection">
              <file original="messages.json" source-language="en-US" target-language="fr-CA" datatype="plaintext">
                <body>
                  <trans-unit id="original" translate="no"><source>Originally excluded</source></trans-unit>
                  <trans-unit id="blackbird" translate="no" bb:excluded="true"><source>Excluded by Blackbird</source></trans-unit>
                </body>
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.RemoveBlackbirdExclusions(Encoding.UTF8.GetBytes(input));
        var units = XDocument.Parse(Encoding.UTF8.GetString(result)).Descendants()
            .Where(x => x.Name.LocalName == "trans-unit")
            .ToDictionary(x => x.Attribute("id")!.Value);

        Assert.AreEqual("no", units["original"].Attribute("translate")?.Value);
        Assert.IsNull(units["blackbird"].Attribute("translate"), Encoding.UTF8.GetString(result));
        Assert.IsFalse(units["blackbird"].Attributes().Any(x => x.Name.LocalName == "excluded"));
        Assert.AreEqual("1.2", XDocument.Parse(Encoding.UTF8.GetString(result)).Root?.Attribute("version")?.Value);
    }

    [TestMethod]
    public void RemoveBlackbirdExclusions_NonXliff_ReturnsOriginalBytes()
    {
        var input = Encoding.UTF8.GetBytes("plain target file");

        var result = XliffSourceSelection.RemoveBlackbirdExclusions(input);

        CollectionAssert.AreEqual(input, result);
    }

    [TestMethod]
    public void Prepare_MultiSegmentUnitWithSameSelection_PreservesBothSegments()
    {
        var input = """
            <xliff srcLang="en-US" trgLang="fr-CA" version="2.1" xmlns="urn:oasis:names:tc:xliff:document:2.0">
              <file id="f1">
                <unit id="u1">
                  <segment id="s1" state="final"><source>First text</source><target>Premier texte</target></segment>
                  <segment id="s2" state="final"><source>Second text</source><target>Deuxième texte</target></segment>
                </unit>
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]);
        var document = XDocument.Parse(Encoding.UTF8.GetString(result.Content));
        var unit = document.Descendants().Single(x => x.Name.LocalName == "unit");
        var segments = unit.Elements().Where(x => x.Name.LocalName == "segment").ToArray();

        Assert.AreEqual(2, result.SegmentsTotal);
        Assert.AreEqual(2, result.SegmentsExcluded);
        Assert.AreEqual(0, result.SegmentsLeft);
        Assert.AreEqual("no", unit.Attribute("translate")?.Value);
        Assert.AreEqual(2, segments.Length);
        CollectionAssert.AreEqual(new[] { "s1", "s2" }, segments.Select(x => x.Attribute("id")!.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "Premier texte", "Deuxième texte" },
            segments.Select(x => x.Elements().Single(e => e.Name.LocalName == "target").Value).ToArray());
    }

    [TestMethod]
    public void Prepare_MixedStateUnit_ThrowsInsteadOfExcludingWantedSegment()
    {
        var input = """
            <xliff srcLang="en-US" trgLang="fr-CA" version="2.1" xmlns="urn:oasis:names:tc:xliff:document:2.0">
              <file id="f1">
                <unit id="u1">
                  <segment state="final"><source>Completed text</source><target>Texte terminé</target></segment>
                  <segment><source>New text</source><target /></segment>
                </unit>
              </file>
            </xliff>
            """;

        var exception = Assert.ThrowsExactly<PluginMisconfigurationException>(() =>
            XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]));

        StringAssert.Contains(exception.Message, "both excluded and translatable segments");
    }

    [TestMethod]
    public void Prepare_MultipleUnexcludedStatesInSameUnit_KeepsAllSegmentsTranslatable()
    {
        var input = """
            <xliff srcLang="en-US" trgLang="fr-CA" version="2.1" xmlns="urn:oasis:names:tc:xliff:document:2.0">
              <file id="f1">
                <unit id="u1">
                  <segment id="s1" state="translated"><source>First text</source><target>Premier texte</target></segment>
                  <segment id="s2" state="initial"><source>Second text</source><target /></segment>
                </unit>
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]);
        var unit = XDocument.Parse(Encoding.UTF8.GetString(result.Content)).Descendants()
            .Single(x => x.Name.LocalName == "unit");

        Assert.AreEqual(2, result.SegmentsTotal);
        Assert.AreEqual(0, result.SegmentsExcluded);
        Assert.AreEqual(2, result.SegmentsLeft);
        Assert.AreEqual(4, result.ApproximateWordCount);
        Assert.IsNull(unit.Attribute("translate"));
    }

    [TestMethod]
    public void Prepare_MissingExclusions_DefaultsToFinalIncludingXliff1SignedOff()
    {
        var input = """
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file original="messages.json" source-language="en-US" target-language="fr-CA" datatype="plaintext">
                <body>
                  <trans-unit id="complete"><source>Completed text</source><target state="signed-off">Texte terminé</target></trans-unit>
                  <trans-unit id="draft"><source>Draft text</source><target state="translated">Brouillon</target></trans-unit>
                </body>
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), null);
        var units = XDocument.Parse(Encoding.UTF8.GetString(result.Content)).Descendants()
            .Where(x => x.Name.LocalName == "unit").ToDictionary(x => x.Attribute("id")!.Value);

        Assert.AreEqual(1, result.SegmentsExcluded);
        Assert.AreEqual(1, result.SegmentsLeft);
        Assert.AreEqual("no", units["complete"].Attribute("translate")?.Value);
        Assert.IsNull(units["draft"].Attribute("translate"));
    }

    [TestMethod]
    public void PrepareAndRemoveBlackbirdExclusions_Xliff21_PreservesOriginalLocks()
    {
        var input = """
            <xliff srcLang="en-US" trgLang="fr-CA" version="2.1" xmlns="urn:oasis:names:tc:xliff:document:2.0">
              <file id="f1">
                <unit id="original" translate="no"><segment state="final"><source>Originally locked</source><target>Verrouillé</target></segment></unit>
                <unit id="selected"><segment state="final"><source>Completed text</source><target>Texte terminé</target></segment></unit>
                <unit id="draft"><segment><source>New text</source><target /></segment></unit>
              </file>
            </xliff>
            """;

        var prepared = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]);
        var result = XliffSourceSelection.RemoveBlackbirdExclusions(prepared.Content);
        var document = XDocument.Parse(Encoding.UTF8.GetString(result));
        var units = document.Descendants().Where(x => x.Name.LocalName == "unit")
            .ToDictionary(x => x.Attribute("id")!.Value);

        Assert.AreEqual(2, prepared.SegmentsExcluded);
        Assert.AreEqual("2.1", document.Root?.Attribute("version")?.Value);
        Assert.AreEqual("no", units["original"].Attribute("translate")?.Value);
        Assert.IsNull(units["selected"].Attribute("translate"));
        Assert.IsNull(units["draft"].Attribute("translate"));
        Assert.IsFalse(units.Values.SelectMany(x => x.Attributes())
            .Any(x => x.Name == XNamespace.Get("https://blackbird.io/xliff/xtm-source-selection") + "excluded"));
    }

    [TestMethod]
    public void PrepareAndRemoveBlackbirdExclusions_InheritedGroupLocks_PreservesOverridesAndMixedStates()
    {
        var input = """
            <xliff srcLang="en-US" trgLang="fr-CA" version="2.1" xmlns="urn:oasis:names:tc:xliff:document:2.0">
              <file id="f1">
                <group id="locked" translate="no">
                  <unit id="inherited">
                    <segment id="s1" state="final"><source>Originally locked</source><target>Verrouillé</target></segment>
                    <segment id="s2"><source>Locked draft</source><target /></segment>
                  </unit>
                  <unit id="explicit" translate="yes"><segment state="final"><source>Explicit override</source><target>Exception explicite</target></segment></unit>
                  <group id="override" translate="yes">
                    <unit id="selected"><segment state="final"><source>Completed text</source><target>Texte terminé</target></segment></unit>
                  </group>
                </group>
                <unit id="draft"><segment><source>New text</source><target /></segment></unit>
              </file>
            </xliff>
            """;

        var prepared = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]);
        var result = XliffSourceSelection.RemoveBlackbirdExclusions(prepared.Content);
        var units = XDocument.Parse(Encoding.UTF8.GetString(result)).Descendants()
            .Where(x => x.Name.LocalName == "unit").ToDictionary(x => x.Attribute("id")!.Value);

        Assert.AreEqual(5, prepared.SegmentsTotal);
        Assert.AreEqual(4, prepared.SegmentsExcluded);
        Assert.AreEqual(1, prepared.SegmentsLeft);
        Assert.AreEqual("no", units["inherited"].AncestorsAndSelf()
            .Select(x => x.Attribute("translate")?.Value).FirstOrDefault(x => x is not null));
        Assert.AreEqual("yes", units["selected"].AncestorsAndSelf()
            .Select(x => x.Attribute("translate")?.Value).FirstOrDefault(x => x is not null));
        Assert.AreEqual("yes", units["explicit"].Attribute("translate")?.Value);
        Assert.IsFalse(units["inherited"].Attributes().Any(x => x.Name.LocalName == "excluded"));
    }

    [TestMethod]
    public void PrepareAndRemoveBlackbirdExclusions_InheritedXliff1FileLock_RemainsLocked()
    {
        var input = """
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file original="locked.json" source-language="en-US" target-language="fr-CA" datatype="plaintext" translate="no">
                <body>
                  <trans-unit id="locked"><source>Originally excluded text</source><target state="signed-off">Texte exclu</target></trans-unit>
                </body>
              </file>
            </xliff>
            """;

        var prepared = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]);
        var result = XliffSourceSelection.RemoveBlackbirdExclusions(prepared.Content);
        var unit = XDocument.Parse(Encoding.UTF8.GetString(result)).Descendants()
            .Single(x => x.Name.LocalName == "unit");

        Assert.AreEqual(1, prepared.SegmentsExcluded);
        Assert.AreEqual("no", unit.AncestorsAndSelf()
            .Select(x => x.Attribute("translate")?.Value).FirstOrDefault(x => x is not null));
        Assert.IsFalse(unit.Attributes().Any(x => x.Name.LocalName == "excluded"));
    }

    [DataTestMethod]
    [DataRow("1.2", "urn:oasis:names:tc:xliff:document:1.2", "trans-unit")]
    [DataRow("2.1", "urn:oasis:names:tc:xliff:document:2.0", "unit")]
    [DataRow("2.2", "urn:oasis:names:tc:xliff:document:2.2", "unit")]
    public void RemoveBlackbirdExclusions_PreservesVersionNamespacesAndUnknownMarkup(
        string version, string xliffNamespace, string unitName)
    {
        var input = $$"""
            <xliff version="{{version}}" xmlns="{{xliffNamespace}}" xmlns:bb="https://blackbird.io/xliff/xtm-source-selection" xmlns:vendor="urn:vendor">
              <file id="f1" original="messages.html" source-language="en-US" datatype="html">
                <vendor:metadata vendor:flag="keep"><vendor:entry>Preserved metadata</vendor:entry></vendor:metadata>
                <vendor:unit translate="no" bb:excluded="true">Vendor metadata</vendor:unit>
                {{(version == "1.2" ? "<body>" : "")}}
                  <{{unitName}} id="original" translate="no"><source>Originally locked</source></{{unitName}}>
                  <{{unitName}} id="selected" translate="no" bb:excluded="true" vendor:flag="keep"><source>Selected <vendor:inline id="x">inline</vendor:inline></source></{{unitName}}>
                  <{{unitName}} id="false" translate="no" bb:excluded="false"><source>Unmarked lock</source></{{unitName}}>
                  <{{unitName}} id="foreign" translate="no" vendor:excluded="true"><source>Vendor lock</source></{{unitName}}>
                {{(version == "1.2" ? "</body>" : "")}}
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.RemoveBlackbirdExclusions(Encoding.UTF8.GetBytes(input));
        var document = XDocument.Parse(Encoding.UTF8.GetString(result));
        var units = document.Descendants().Where(x => x.Name == XNamespace.Get(xliffNamespace) + unitName)
            .ToDictionary(x => x.Attribute("id")!.Value);
        XNamespace vendor = "urn:vendor";
        XNamespace blackbird = "https://blackbird.io/xliff/xtm-source-selection";

        Assert.AreEqual(version, document.Root?.Attribute("version")?.Value);
        Assert.AreEqual(xliffNamespace, document.Root?.Name.NamespaceName);
        Assert.AreEqual("urn:vendor", document.Root?.GetNamespaceOfPrefix("vendor")?.NamespaceName);
        Assert.AreEqual("Preserved metadata", document.Descendants(vendor + "entry").Single().Value);
        Assert.AreEqual("inline", units["selected"].Descendants(vendor + "inline").Single().Value);
        Assert.AreEqual("keep", units["selected"].Attribute(vendor + "flag")?.Value);
        Assert.AreEqual("no", units["original"].Attribute("translate")?.Value);
        Assert.IsNull(units["selected"].Attribute("translate"));
        Assert.IsNull(units["selected"].Attribute(blackbird + "excluded"));
        Assert.AreEqual("no", units["false"].Attribute("translate")?.Value);
        Assert.AreEqual("false", units["false"].Attribute(blackbird + "excluded")?.Value);
        Assert.AreEqual("no", units["foreign"].Attribute("translate")?.Value);
        Assert.AreEqual("true", units["foreign"].Attribute(vendor + "excluded")?.Value);
        Assert.AreEqual("no", document.Descendants(vendor + "unit").Single().Attribute("translate")?.Value);
        Assert.AreEqual("true", document.Descendants(vendor + "unit").Single().Attribute(blackbird + "excluded")?.Value);
    }

    [TestMethod]
    public void RemoveBlackbirdExclusions_Utf16Xliff_PreservesUnicodeAndReturnsReadableXml()
    {
        var input = """
            <?xml version="1.0" encoding="utf-16"?>
            <xliff version="2.1" srcLang="en" trgLang="ja" xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:bb="https://blackbird.io/xliff/xtm-source-selection">
              <file id="f1"><unit id="u1" translate="no" bb:excluded="true"><segment><source>Café &amp; crème</source><target>日本語 – 翻訳</target></segment></unit></file>
            </xliff>
            """;
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(input)).ToArray();

        var result = XliffSourceSelection.RemoveBlackbirdExclusions(bytes);
        using var stream = new MemoryStream(result);
        var document = XDocument.Load(stream);

        Assert.AreEqual("2.1", document.Root?.Attribute("version")?.Value);
        Assert.AreEqual("Café & crème", document.Descendants().Single(x => x.Name.LocalName == "source").Value);
        Assert.AreEqual("日本語 – 翻訳", document.Descendants().Single(x => x.Name.LocalName == "target").Value);
        Assert.IsNull(document.Descendants().Single(x => x.Name.LocalName == "unit").Attribute("translate"));
    }

    [TestMethod]
    public void RemoveBlackbirdExclusions_UnmarkedXliff_ReturnsOriginalBytes()
    {
        var input = Encoding.UTF8.GetBytes("""
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="2.1" srcLang="en" xmlns="urn:oasis:names:tc:xliff:document:2.0">
                <file id="f1"><unit id="u1" translate="no"><segment><source>Original lock</source></segment></unit></file>
            </xliff>
            """);

        var result = XliffSourceSelection.RemoveBlackbirdExclusions(input);

        CollectionAssert.AreEqual(input, result);
    }
}
