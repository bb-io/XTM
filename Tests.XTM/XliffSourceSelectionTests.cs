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
        Assert.IsNull(units["u2"].Attribute("translate"));
        Assert.AreEqual("no", units["u3"].Attribute("translate")?.Value);
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
