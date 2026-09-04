using Apps.XTM.Utils;
using Blackbird.Applications.Sdk.Common.Exceptions;
using System.Text;
using System.Xml.Linq;

namespace Tests.XTM;

[TestClass]
public class XliffSourceSelectionTests
{
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
            <xliff srcLang="en-US" trgLang="fr-CA" version="2.2" xmlns="urn:oasis:names:tc:xliff:document:2.2">
              <file id="f1">
                <unit id="u1"><segment state="final"><source>Already translated text</source><target>Texte traduit</target></segment></unit>
                <unit id="u2"><segment><source>New text to translate</source><target /></segment></unit>
                <unit id="u3"><segment state="final"><source>Another completed string</source><target>Une autre chaîne</target></segment></unit>
              </file>
            </xliff>
            """;

        var result = XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]);
        var document = XDocument.Parse(Encoding.UTF8.GetString(result.Content));
        var units = document.Descendants().Where(x => x.Name.LocalName == "trans-unit").ToDictionary(
            x => x.Attribute("id")!.Value);

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
        StringAssert.Contains(Encoding.UTF8.GetString(result.Content), "bb:excluded=\"true\"");
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
        var units = document.Descendants().Where(x => x.Name.LocalName == "trans-unit").ToDictionary(
            x => x.Attribute("id")!.Value);

        Assert.AreEqual("1.2", document.Root?.Attribute("version")?.Value);
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
        Assert.DoesNotContain("xliff/xtm-source-selection", Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void RemoveBlackbirdExclusions_NonXliff_ReturnsOriginalBytes()
    {
        var input = Encoding.UTF8.GetBytes("plain target file");

        var result = XliffSourceSelection.RemoveBlackbirdExclusions(input);

        CollectionAssert.AreEqual(input, result);
    }

    [TestMethod]
    public void Prepare_MultiSegmentUnit_ThrowsInsteadOfUsingLossyConversion()
    {
        var input = """
            <xliff srcLang="en-US" trgLang="fr-CA" version="2.2" xmlns="urn:oasis:names:tc:xliff:document:2.2">
              <file id="f1">
                <unit id="u1">
                  <segment id="s1" state="final"><source>First text</source><target>Premier texte</target></segment>
                  <segment id="s2" state="final"><source>Second text</source><target>Deuxième texte</target></segment>
                </unit>
              </file>
            </xliff>
            """;

        var exception = Assert.ThrowsExactly<PluginMisconfigurationException>(() =>
            XliffSourceSelection.Prepare(Encoding.UTF8.GetBytes(input), ["final"]));

        StringAssert.Contains(exception.Message, "multiple segments");
    }

    [TestMethod]
    public void Prepare_MixedStateUnit_ThrowsInsteadOfExcludingWantedSegment()
    {
        var input = """
            <xliff srcLang="en-US" trgLang="fr-CA" version="2.2" xmlns="urn:oasis:names:tc:xliff:document:2.2">
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

        StringAssert.Contains(exception.Message, "multiple segments");
    }
}
