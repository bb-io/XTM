using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Apps.XTM.Actions;
using Apps.XTM.Models.Response.Workflows;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.XTM;

[TestClass]
public class XtmProvenanceTests
{
    private static readonly Func<byte[], byte[], string, string, IReadOnlyList<WorkflowAssignmentBundleResponse>, byte[]> ApplyProvenance =
        typeof(InteroperableActions).GetMethod("ApplyProvenance", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate<Func<byte[], byte[], string, string, IReadOnlyList<WorkflowAssignmentBundleResponse>, byte[]>>();

    [TestMethod]
    [DataRow("live-review-initial", "only_confirmed", "translation", "")]
    [DataRow("live-review-translated", "only_confirmed", "translation", "")]
    [DataRow("live-review-review-start", "only_confirmed", "review", "")]
    [DataRow("live-review-reviewed", "only_confirmed", "review", "unit-change,unit-confirm,unit-multi")]
    [DataRow("live-review-reviewed-all", "only_confirmed", "review", "unit-change,unit-confirm,unit-multi")]
    [DataRow("live-review-reviewed", "only_changed", "review", "")]
    [DataRow("live-correct-initial", "only_confirmed", "translation", "")]
    [DataRow("live-correct-translated", "only_confirmed", "translation", "")]
    [DataRow("live-correct-corrected", "only_confirmed", "review", "unit-change")]
    // The untouched, source-populated unit differs from an older fuzzy suggestion: this mode is not edit history.
    [DataRow("live-correct-corrected", "only_changed", "review", "unit-empty,unit-change")]
    [DataRow("live-correct-corrected", "none", "review", "")]
    [DataRow("live-review-reviewed", "all", "review", "unit-empty,unit-change,unit-confirm,unit-new,unit-multi")]
    public void Apply_LiveExportPairs_MapRenumberedSegmentsToOriginalUnits(
        string fixture, string mode, string provenanceType, string attributedUnits)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "TestFiles", "Input", "Provenance");
        var target = XDocument.Load(Path.Combine(directory, fixture + "-target.xlf"), LoadOptions.PreserveWhitespace);
        var offline = File.ReadAllBytes(Path.Combine(directory, fixture + "-offline.xlf"));
        XNamespace its = "http://www.w3.org/2005/11/its";
        XNamespace xliff = "urn:oasis:names:tc:xliff:document:2.0";
        target.Root!.SetAttributeValue(XNamespace.Xmlns + "its", its.NamespaceName);
        if (provenanceType == "review")
        {
            foreach (var unit in target.Descendants(xliff + "unit"))
            {
                unit.SetAttributeValue(its + "person", "Earlier translator");
                unit.SetAttributeValue(its + "tool", "Original translation tool");
            }
        }
        var output = XDocument.Parse(Encoding.UTF8.GetString(ApplyProvenance(
            Encoding.UTF8.GetBytes(target.ToString(SaveOptions.DisableFormatting)), offline, mode, provenanceType,
            [new WorkflowAssignmentBundleResponse { From = 1, To = 6, UserId = "9001", UserName = "Fixture reviewer" }])),
            LoadOptions.PreserveWhitespace);
        var expectedPeople = attributedUnits.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var personAttribute = its + (provenanceType == "review" ? "revPerson" : "person");
        var toolAttribute = its + (provenanceType == "review" ? "revTool" : "tool");
        CollectionAssert.AreEquivalent(expectedPeople,
            output.Descendants(xliff + "unit").Where(x => x.Attribute(personAttribute) is not null)
                .Select(x => x.Attribute("id")!.Value).ToArray());
        foreach (var unit in output.Descendants(xliff + "unit"))
        {
            var id = unit.Attribute("id")!.Value;
            Assert.AreEqual(expectedPeople.Contains(id) ? "Fixture reviewer (ID 9001)" : null,
                unit.Attribute(personAttribute)?.Value, id);
            Assert.IsFalse(unit.Attribute(personAttribute)?.Value.Contains("1001", StringComparison.Ordinal) == true,
                "The fuzzy suggestion's xtm:changedby author must not become the current unit's author.");
            StringAssert.StartsWith(unit.Attribute(toolAttribute)!.Value, "XTM", id);
            if (provenanceType == "review")
            {
                Assert.AreEqual("Earlier translator", unit.Attribute(its + "person")?.Value, id);
                Assert.AreEqual("Original translation tool", unit.Attribute(its + "tool")?.Value, id);
            }
            var original = target.Descendants(xliff + "unit").Single(x => x.Attribute("id")!.Value == id);
            CollectionAssert.AreEqual(original.Elements().Select(x => x.ToString(SaveOptions.DisableFormatting)).ToArray(),
                unit.Elements().Select(x => x.ToString(SaveOptions.DisableFormatting)).ToArray(),
                "Provenance must preserve original segment IDs and content.");
        }
        Assert.AreEqual(target.Root.Attribute("version")!.Value, output.Root!.Attribute("version")!.Value);
        Assert.AreEqual(target.Root.Name, output.Root.Name);
        Assert.AreEqual(target.Root.Attribute("srcLang")?.Value, output.Root.Attribute("srcLang")?.Value);
        Assert.AreEqual(target.Root.Attribute("trgLang")?.Value, output.Root.Attribute("trgLang")?.Value);
    }

    [TestMethod]
    [DataRow("all", "translated", "Hallo", true)]
    [DataRow("only_confirmed", "signed-off", "Hallo", true)]
    [DataRow("only_confirmed", "SIGNED-OFF", "Hallo", true)]
    [DataRow("only_confirmed", "translated", "Hallo", false)]
    [DataRow("only_changed", "signed-off", "Hallo", false)]
    [DataRow("only_changed", "translated", "Guten Tag", true)]
    [DataRow("only_changed", "translated", "", true)]
    [DataRow("only_changed", "signed-off", null, false)]
    [DataRow("none", "signed-off", "Guten Tag", false)]
    public void Apply_AttributionModes_UseConfirmationAndSelectedSuggestion(
        string mode, string state, string? selectedBaseline, bool expectedPerson)
    {
        const string target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:its="http://www.w3.org/2005/11/its" version="2.1" srcLang="en" trgLang="de">
              <file id="f1"><unit id="u1" its:person="Stale person" its:personRef="stale-person" its:toolRef="stale-tool"><segment id="s1"><source>Hello</source><target>Hallo</target></segment></unit></file>
            </xliff>
            """;
        XNamespace xtm = "urn:oasis:names:tc:xliff:document:1.2";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var offlineUnit = new XElement(xtm + "trans-unit", new XAttribute("id", "t1"),
            new XElement(xtm + "source", "Hello"),
            new XElement(xtm + "target", new XAttribute("state", state),
                new XAttribute("state-qualifier", "mt-suggestion"), "Hallo"),
            // A different suggestion must not establish whether the selected MT suggestion changed.
            new XElement(xtm + "alt-trans", new XAttribute("extype", "exact-match"), new XElement(xtm + "target", "Wrong baseline")));
        if (selectedBaseline is not null)
            offlineUnit.Add(new XElement(xtm + "alt-trans", new XAttribute("extype", "MACHINE-TRANSLATION"),
                new XElement(xtm + "target", selectedBaseline)));
        var offline = new XElement(xtm + "xliff", new XAttribute("version", "1.2"),
            new XElement(xtm + "file", new XAttribute("source-language", "en"), new XAttribute("target-language", "de"),
                new XElement(xtm + "body", offlineUnit)));
        var output = XDocument.Parse(Encoding.UTF8.GetString(ApplyProvenance(Encoding.UTF8.GetBytes(target),
            Encoding.UTF8.GetBytes(offline.ToString()), mode, "translation",
            [new WorkflowAssignmentBundleResponse { From = 1, To = 1, UserId = "42", UserName = "Assigned linguist" }])));
        var unit = output.Descendants().Single(x => x.Name.LocalName == "unit");
        Assert.AreEqual(expectedPerson ? "Assigned linguist (ID 42)" : null, unit.Attribute(its + "person")?.Value);
        Assert.AreEqual(expectedPerson ? "XTM" : "XTM (mt suggestion)", unit.Attribute(its + "tool")?.Value);
        Assert.IsNull(unit.Attribute(its + "personRef"));
        Assert.IsNull(unit.Attribute(its + "toolRef"));
    }

    [TestMethod]
    [DataRow("translation")]
    [DataRow("review")]
    public void Apply_UpdatesSelectedRoleAndPreservesOtherProvenance(string provenanceType)
    {
        const string target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:its="http://www.w3.org/2005/11/its" version="2.1" srcLang="en" trgLang="de">
              <file id="f"><unit id="u"
                its:person="  Earlier  translator  " its:personRef="  translator-ref&#x9;  "
                its:tool="  Translation  tool  " its:toolRef="  translation-tool-ref&#xA;  "
                its:org="  Translation  organization  " its:orgRef="  translation-org-ref  "
                its:revPerson="  Earlier  reviewer  " its:revPersonRef="  reviewer-ref&#x9;  "
                its:revTool="  Review  tool  " its:revToolRef="  review-tool-ref&#xA;  "
                its:revOrg="  Review  organization  " its:revOrgRef="  review-org-ref  "
                its:customAttribute="  Unknown &amp; preserved  ">
                <!-- Keep the original unit content and whitespace. -->
                <segment id="s"><source>Hello</source><target>Hallo</target></segment>
              </unit></file>
            </xliff>
            """;
        const string offline = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2"><file source-language="en" target-language="de"><body>
              <trans-unit id="t1"><source>Hello</source><target state="signed-off">Hallo</target></trans-unit>
            </body></file></xliff>
            """;
        const string assignee = "Assigned & <linguist> \"Q\" 'A'";
        var original = XDocument.Parse(target, LoadOptions.PreserveWhitespace);
        var output = XDocument.Parse(Encoding.UTF8.GetString(ApplyProvenance(
            Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "all", provenanceType,
            [new WorkflowAssignmentBundleResponse { From = 1, To = 1, UserId = "42", UserName = assignee }])),
            LoadOptions.PreserveWhitespace);
        XNamespace xliff = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var originalUnit = original.Descendants(xliff + "unit").Single();
        var updatedUnit = output.Descendants(xliff + "unit").Single();
        string[] selectedAttributes = provenanceType == "review"
            ? ["revPerson", "revPersonRef", "revTool", "revToolRef"]
            : ["person", "personRef", "tool", "toolRef"];

        Assert.AreEqual($"{assignee} (ID 42)", updatedUnit.Attribute(its + selectedAttributes[0])?.Value);
        Assert.IsNull(updatedUnit.Attribute(its + selectedAttributes[1]));
        Assert.AreEqual("XTM", updatedUnit.Attribute(its + selectedAttributes[2])?.Value);
        Assert.IsNull(updatedUnit.Attribute(its + selectedAttributes[3]));
        foreach (var attribute in originalUnit.Attributes().Where(x =>
                     x.Name.Namespace != its || !selectedAttributes.Contains(x.Name.LocalName)))
            Assert.AreEqual(attribute.Value, updatedUnit.Attribute(attribute.Name)?.Value,
                $"Attribute {attribute.Name} must retain its exact value, including whitespace.");

        foreach (var attribute in selectedAttributes)
        {
            originalUnit.Attribute(its + attribute)?.Remove();
            updatedUnit.Attribute(its + attribute)?.Remove();
        }
        Assert.IsTrue(XNode.DeepEquals(original, output), "Only the selected role's person and tool attributes may change.");
    }

    [TestMethod]
    [DataRow("Hallo <ph id=\"last\"/><ph id=\"first\"/>",
        "Hallo <ph id=\"2\">{lastName}</ph><ph id=\"1\">{firstName}</ph>",
        "Hallo <ph id=\"1\">{firstName}</ph><ph id=\"2\">{lastName}</ph>", true)]
    [DataRow("Hallo <ph id=\"name\"/>",
        "Hallo <ph id=\"1\">{lastName}</ph>",
        "Hallo <ph id=\"1\">{firstName}</ph>", true)]
    [DataRow("<pc id=\"style\">Hallo</pc>",
        "<bpt id=\"1\">&lt;i&gt;</bpt>Hallo<ept id=\"1\">&lt;/i&gt;</ept>",
        "<bpt id=\"1\">&lt;b&gt;</bpt>Hallo<ept id=\"1\">&lt;/b&gt;</ept>", true)]
    [DataRow("Hallo <ph id=\"name\"/>",
        "Hallo <ph id=\"10\">{name}</ph>",
        "Hallo <ph id=\"98\">{name}</ph>", false)]
    [DataRow("<pc id=\"style\">Hallo</pc>",
        "<bpt id=\"10\">&lt;b&gt;</bpt>Hallo<ept id=\"10\">&lt;/b&gt;</ept>",
        "<bpt id=\"98\">&lt;b&gt;</bpt>Hallo<ept id=\"98\">&lt;/b&gt;</ept>", false)]
    [DataRow("Hallo <ph id=\"name\"/>",
        "Hallo <ph id=\"10\">{name}</ph>",
        "Hallo <ph id=\"10\">{name}</ph>", false)]
    [DataRow("Hallo <ph id=\"name\"/>",
        "Hallo <ph id=\"10\">{name}</ph>",
        "Guten Tag <ph id=\"98\">{name}</ph>", true)]
    [DataRow("Hallo <ph id=\"last\"/><ph id=\"first\"/>",
        "Hallo <ph id=\"2\"/><ph id=\"1\"/>",
        "Hallo <ph id=\"1\"/><ph id=\"2\"/>", true)]
    [DataRow("Hallo <ph id=\"name\"/>",
        "Hallo <ph id=\"2\"/>",
        "Hallo <ph id=\"1\"/>", true)]
    [DataRow("Hallo <ph id=\"name\"/>",
        "Hallo <ph id=\"1\"/>",
        "Hallo <ph id=\"1\"/>", false)]
    [DataRow("Hallo <ph id=\"name\"/>",
        "Hallo <ph id=\"1\"/>",
        "Hallo <ph id=\"1\"></ph>", false)]
    [DataRow("<ph id=\"left\"/> <ph id=\"right\"/>",
        "<ph id=\"10\">{left}</ph> <ph id=\"20\">{right}</ph>",
        "<ph id=\"98\">{left}</ph> <ph id=\"99\">{right}</ph>", false)]
    [DataRow("<ph id=\"left\"/> <ph id=\"right\"/>",
        "<ph id=\"10\">{left}</ph> <ph id=\"20\">{right}</ph>",
        "<ph id=\"98\">{left}</ph><ph id=\"99\">{right}</ph>", true)]
    [DataRow("<ph id=\"left\"/> <ph id=\"right\"/>",
        "<ph id=\"10\">{left}</ph> <ph id=\"20\">{right}</ph>",
        "<ph id=\"98\">{left}</ph>\t<ph id=\"99\">{right}</ph>", true)]
    public void Apply_OnlyChanged_UsesNativeInlineContentAndEmptyPlaceholderIdentity(
        string translatedContent, string currentContent, string baselineContent, bool expectedPerson)
    {
        var target = $"""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en" trgLang="de">
              <file id="f"><unit id="u"><segment id="s">
                <source>{translatedContent.Replace("Hallo", "Hello", StringComparison.Ordinal)}</source>
                <target>{translatedContent}</target>
              </segment></unit></file>
            </xliff>
            """;
        var offline = $"""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2"><file source-language="en" target-language="de"><body><trans-unit id="t1">
              <source>{currentContent.Replace("Hallo", "Hello", StringComparison.Ordinal)}</source>
              <target state="signed-off" state-qualifier="mt-suggestion">{currentContent}</target>
              <alt-trans extype="MACHINE-TRANSLATION"><target>{baselineContent}</target></alt-trans>
            </trans-unit></body></file></xliff>
            """;

        var output = XDocument.Parse(Encoding.UTF8.GetString(ApplyProvenance(
            Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "only_changed", "translation",
            [new WorkflowAssignmentBundleResponse { From = 1, To = 1, UserId = "42", UserName = "Assigned linguist" }])));

        XNamespace xliff = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var unit = output.Descendants(xliff + "unit").Single();
        Assert.AreEqual(expectedPerson ? "Assigned linguist (ID 42)" : null, unit.Attribute(its + "person")?.Value);
        Assert.AreEqual(expectedPerson ? "XTM" : "XTM (mt suggestion)", unit.Attribute(its + "tool")?.Value);
    }

    [TestMethod]
    [DataRow("split-assignees", null)]
    [DataRow("partial-confirmation", null)]
    [DataRow("same-assignee", "First linguist (ID 42)")]
    [DataRow("overlap", "error")]
    public void Apply_MultisegmentUnits_RequireOnePersonForEverySegment(string scenario, string? expectedPerson)
    {
        const string target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en" trgLang="de">
              <file id="f"><unit id="u"><segment id="s1"><source>One</source><target>Eins</target></segment><segment id="s2"><source>Two</source><target>Zwei</target></segment></unit></file>
            </xliff>
            """;
        var offline = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2"><file source-language="en" target-language="de"><body>
              <trans-unit id="t1"><source>One</source><target state="signed-off">Eins</target></trans-unit>
              <trans-unit id="t2"><source>Two</source><target state="signed-off">Zwei</target></trans-unit>
            </body></file></xliff>
            """;
        if (scenario == "partial-confirmation")
            offline = offline.Replace("<target state=\"signed-off\">Zwei", "<target state=\"translated\">Zwei", StringComparison.Ordinal);
        var bundles = new List<WorkflowAssignmentBundleResponse>
        {
            new() { From = 1, To = scenario is "same-assignee" or "partial-confirmation" or "overlap" ? 2 : 1,
                UserId = "42", UserName = "First linguist" },
        };
        if (scenario is "split-assignees" or "overlap")
            bundles.Add(new() { From = 2, To = 2, UserId = "43", UserName = "Second linguist" });
        if (scenario == "overlap")
        {
            var exception = Assert.Throws<PluginApplicationException>(() => ApplyProvenance(
                Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "only_confirmed", "translation", bundles));
            StringAssert.Contains(exception.Message, "overlapping assignees for segment 2");
        }
        else
        {
            var output = XDocument.Parse(Encoding.UTF8.GetString(ApplyProvenance(
                Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "only_confirmed", "translation", bundles)));
            XNamespace its = "http://www.w3.org/2005/11/its";
            var unit = output.Descendants().Single(x => x.Name.LocalName == "unit");
            Assert.AreEqual(expectedPerson, unit.Attribute(its + "person")?.Value);
            Assert.AreEqual("XTM", unit.Attribute(its + "tool")?.Value);
        }
    }

    [TestMethod]
    public void Apply_PreservesExcludedUnitsIgnorablesMetadataAndInlineCodes()
    {
        const string target = """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:its="http://www.w3.org/2005/11/its" xmlns:mda="urn:oasis:names:tc:xliff:metadata:2.0" version="2.1" srcLang="en_GB" trgLang="de_DE">
              <file id="f" original="content.html"><skeleton href="content.bin" />
                <group id="g" translate="no"><unit id="excluded" its:person="Original author"><segment id="excluded-s"><source>Excluded</source><target>Ausgeschlossen</target></segment></unit></group>
                <unit id="u"><mda:metadata><mda:metaGroup category="blackbird"><mda:meta type="custom">keep</mda:meta></mda:metaGroup></mda:metadata>
                  <originalData><data id="bold">&lt;b&gt;</data></originalData>
                  <segment id="s"><source xml:space="preserve"> Hello <pc id="b" dataRefStart="bold">world</pc><ph id="p"/>! </source><target xml:space="preserve"> Hallo <pc id="b" dataRefStart="bold">Welt</pc><ph id="p"/>! </target></segment>
                  <ignorable id="i"><source> </source><target> </target></ignorable>
                </unit>
              </file>
            </xliff>
            """;
        const string offline = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2"><file source-language="en-GB" target-language="de-DE"><body><trans-unit id="t1">
              <source xml:space="preserve"> Hello <bpt id="99">&lt;b&gt;</bpt>world<ept id="99">&lt;/b&gt;</ept><ph id="101">&lt;br/&gt;</ph>! </source>
              <target xml:space="preserve"> Hallo <bpt id="99">&lt;b&gt;</bpt>Welt<ept id="99">&lt;/b&gt;</ept><ph id="101">&lt;br/&gt;</ph>! </target>
            </trans-unit></body></file></xliff>
            """;
        var original = XDocument.Parse(target, LoadOptions.PreserveWhitespace);
        var result = ApplyProvenance(Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "none", "translation", []);
        var output = XDocument.Parse(Encoding.UTF8.GetString(result), LoadOptions.PreserveWhitespace);
        XNamespace its = "http://www.w3.org/2005/11/its";
        var updated = output.Descendants().Single(x => (string?)x.Attribute("id") == "u");
        Assert.AreEqual("XTM", updated.Attribute(its + "tool")?.Value);
        updated.Attribute(its + "tool")!.Remove();
        Assert.IsTrue(XNode.DeepEquals(original, output), "Only unit provenance attributes may change.");
        Assert.AreEqual(original.Declaration!.Version, output.Declaration!.Version);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Apply_ReusedUnitIdsAndSources_ValidatesTargetsAcrossFileScopes(bool swappedTargets)
    {
        const string target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en" trgLang="de">
              <file id="first"><unit id="same"><segment id="s"><source>Hello</source><target>Hallo</target></segment></unit></file>
              <file id="second"><unit id="same"><segment id="s"><source>Hello</source><target>Guten Tag</target></segment></unit></file>
            </xliff>
            """;
        var offline = $"""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2"><file source-language="en" target-language="de"><body>
              <trans-unit id="t1"><source>Hello</source><target>{(swappedTargets ? "Guten Tag" : "Hallo")}</target></trans-unit>
              <trans-unit id="t2"><source>Hello</source><target>{(swappedTargets ? "Hallo" : "Guten Tag")}</target></trans-unit>
            </body></file></xliff>
            """;
        var bundles = new[]
        {
            new WorkflowAssignmentBundleResponse { From = 1, To = 1, UserId = "1", UserName = "First" },
            new WorkflowAssignmentBundleResponse { From = 2, To = 2, UserId = "2", UserName = "Second" },
        };
        if (swappedTargets)
        {
            var exception = Assert.Throws<PluginApplicationException>(() => ApplyProvenance(
                Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "all", "review", bundles));
            StringAssert.Contains(exception.Message, "Segment 1 differs");
        }
        else
        {
            var output = XDocument.Parse(Encoding.UTF8.GetString(ApplyProvenance(
                Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "all", "review", bundles)));
            XNamespace its = "http://www.w3.org/2005/11/its";
            var units = output.Descendants().Where(x => x.Name.LocalName == "unit").ToArray();
            Assert.AreEqual("First (ID 1)", (string?)units[0].Attribute(its + "revPerson"));
            Assert.AreEqual("Second (ID 2)", (string?)units[1].Attribute(its + "revPerson"));
        }
    }

    [TestMethod]
    [DataRow("first-populates", null)]
    [DataRow("second-populates", null)]
    [DataRow("prefixed", null)]
    [DataRow("segmented", null)]
    [DataRow("annotation", null)]
    [DataRow("first-missing-populate", "Segment 1 differs")]
    [DataRow("second-missing-populate", "Segment 6 differs")]
    [DataRow("source-language", null)]
    [DataRow("target-language", null)]
    [DataRow("missing-source-language", null)]
    [DataRow("missing-target-language", null)]
    public void Apply_OfflineGroupsAndFiles_PreserveOrderAssignmentsAndFileSettings(string scenario, string? expectedError)
    {
        const string target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en" trgLang="de"><file id="f">
              <unit id="u1"><segment id="s1"><source>One</source><target>One</target></segment></unit>
              <unit id="u2"><segment id="s2"><source>Two</source><target>Zwei</target></segment></unit>
              <unit id="u3"><segment id="s3"><source>Three</source><target>Drei</target></segment></unit>
              <unit id="u4"><segment id="s4"><source>Four</source><target>Vier</target></segment></unit>
              <unit id="u5"><segment id="s5"><source>Five</source><target>Fünf</target></segment></unit>
              <unit id="u6"><segment id="s6"><source>Six</source><target>Six</target></segment></unit>
            </file></xliff>
            """;
        var firstPopulates = scenario is "first-populates" or "second-missing-populate";
        var secondPopulates = !firstPopulates;
        var offline = XDocument.Parse($"""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:xtm="urn:xliff-xtm-extensions" version="1.2">
              <file original="first" source-language="en" target-language="de" xtm:populate-target-with-source="{(firstPopulates ? "yes" : "no")}"><body>
                <trans-unit id="t9"><source>One</source><target>{(firstPopulates ? "" : "One")}</target></trans-unit>
                <group id="outer">
                  <trans-unit id="t3"><source>Two</source><target>Zwei</target></trans-unit>
                  <group id="inner"><trans-unit id="t12"><source>Three</source><target>Drei</target></trans-unit></group>
                  <trans-unit id="t6"><source>Four</source><target>Vier</target></trans-unit>
                </group>
                <trans-unit id="t4"><source>Five</source><target>Fünf</target></trans-unit>
              </body></file>
              <file original="second" source-language="en" target-language="de" xtm:populate-target-with-source="{(secondPopulates ? "yes" : "no")}"><body>
                <trans-unit id="t21"><source>Six</source><target>{(secondPopulates ? "" : "Six")}</target></trans-unit>
              </body></file>
            </xliff>
            """, LoadOptions.PreserveWhitespace);
        XNamespace xtm = "urn:oasis:names:tc:xliff:document:1.2";
        if (scenario.Contains("language", StringComparison.Ordinal))
            offline.Root!.Elements(xtm + "file").Last().SetAttributeValue(
                scenario.Replace("missing-", "", StringComparison.Ordinal),
                scenario.StartsWith("missing-", StringComparison.Ordinal) ? null : "fr");
        else if (scenario is "first-missing-populate" or "second-missing-populate")
            offline.Descendants(xtm + "trans-unit")
                .Single(x => (string?)x.Attribute("id") == (scenario == "first-missing-populate" ? "t9" : "t21"))
                .Element(xtm + "target")!.RemoveNodes();
        else if (scenario == "prefixed")
        {
            offline.Root!.Attribute("xmlns")!.Remove();
            offline.Root.SetAttributeValue(XNamespace.Xmlns + "x", xtm.NamespaceName);
        }
        else if (scenario is "segmented" or "annotation")
        {
            var unit = offline.Descendants(xtm + "trans-unit").Single(x => (string?)x.Attribute("id") == "t3");
            if (scenario == "segmented")
                unit.Add(new XElement(xtm + "seg-source", new XElement(xtm + "mrk",
                    new XAttribute("mtype", "seg"), new XAttribute("mid", "1"), "Two")));
            foreach (var element in unit.Elements().Where(x => x.Name == xtm + "source" || x.Name == xtm + "target"))
            {
                var marker = new XElement(xtm + "mrk", new XAttribute("mtype", scenario == "segmented" ? "seg" : "comment"),
                    new XAttribute("mid", "1"), element.Value);
                if (scenario == "annotation")
                    marker.SetAttributeValue(XNamespace.Get("http://blackbird.io/") + "position", "x-start");
                element.ReplaceNodes(marker);
            }
        }
        int[] positions = [9, 3, 12, 6, 4, 21];
        var bundles = positions.Select(position => new WorkflowAssignmentBundleResponse
        {
            From = position, To = position, UserId = position.ToString(), UserName = $"Linguist {position}",
        }).ToArray();
        var offlineBytes = Encoding.UTF8.GetBytes(offline.ToString(SaveOptions.DisableFormatting));

        if (expectedError is not null)
        {
            var exception = Assert.Throws<PluginApplicationException>(() => ApplyProvenance(
                Encoding.UTF8.GetBytes(target), offlineBytes, "all", "translation", bundles));
            StringAssert.Contains(exception.Message, expectedError);
            return;
        }

        var output = XDocument.Parse(Encoding.UTF8.GetString(ApplyProvenance(
            Encoding.UTF8.GetBytes(target), offlineBytes, "all", "translation", bundles)));
        XNamespace xliff = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var units = output.Descendants(xliff + "unit").ToArray();
        Assert.AreEqual("en", (string?)output.Root!.Attribute("srcLang"));
        Assert.AreEqual("de", (string?)output.Root.Attribute("trgLang"));
        Assert.HasCount(positions.Length, units);
        for (var index = 0; index < positions.Length; index++)
        {
            Assert.AreEqual($"u{index + 1}", (string?)units[index].Attribute("id"));
            Assert.AreEqual($"Linguist {positions[index]} (ID {positions[index]})", (string?)units[index].Attribute(its + "person"));
        }
    }

    [TestMethod]
    [DataRow("source", "Segment 1 differs")]
    [DataRow("missing-source", "Segment 1 differs")]
    [DataRow("inherited-space", "Segment 1 differs")]
    [DataRow("target", "Segment 1 differs")]
    [DataRow("count", "translatable segments")]
    [DataRow("inline-code", "Segment 1 differs")]
    public void Apply_RejectsMismatchedExports(string mismatch, string expectedError)
    {
        const string target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en" trgLang="de"><file id="f"><unit id="u"><segment id="s"><source>Hello</source><target>Hallo</target></segment></unit></file></xliff>
            """;
        var offline = XDocument.Parse("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2"><file source-language="en" target-language="de"><body><trans-unit id="t1"><source>Hello</source><target>Hallo</target></trans-unit></body></file></xliff>
            """);
        var element = offline.Descendants().FirstOrDefault(x => x.Name.LocalName == mismatch);
        if (mismatch is "source" or "target")
            element!.Value = "Changed";
        else if (mismatch == "missing-source")
            offline.Descendants().Single(x => x.Name.LocalName == "source").Remove();
        else if (mismatch == "inherited-space")
        {
            offline.Descendants().Single(x => x.Name.LocalName == "body").SetAttributeValue(XNamespace.Xml + "space", "preserve");
            offline.Descendants().Single(x => x.Name.LocalName == "source").Value = " Hello ";
        }
        else if (mismatch == "count")
            offline.Descendants().Single(x => x.Name.LocalName == "trans-unit").Remove();
        else if (mismatch == "inline-code")
            offline.Descendants().Single(x => x.Name.LocalName == "source").Add(new XElement(offline.Root!.Name.Namespace + "ph", new XAttribute("id", "1")));
        var offlineBytes = Encoding.UTF8.GetBytes(offline.ToString());
        var exception = Assert.Throws<PluginApplicationException>(() => ApplyProvenance(
            Encoding.UTF8.GetBytes(target), offlineBytes, "none", "translation", []));
        StringAssert.Contains(exception.Message, expectedError);
    }

    [TestMethod]
    [DataRow("target", "malformed", "invalid XLIFF")]
    [DataRow("offline", "malformed", "invalid XLIFF")]
    [DataRow("target", "non-xliff", "invalid XLIFF")]
    [DataRow("offline", "non-xliff", "invalid XLIFF")]
    [DataRow("target", "wrong-format", "Provenance requires the translated XLIFF 2 file and XTM's offline XLIFF 1.2 file.")]
    [DataRow("offline", "wrong-format", "Provenance requires the translated XLIFF 2 file and XTM's offline XLIFF 1.2 file.")]
    public void Apply_RejectsUnloadableOrWrongFormatExports(string file, string content, string expectedError)
    {
        var target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1" srcLang="en" trgLang="de"><file id="f"><unit id="u"><segment id="s"><source>Hello</source><target>Hallo</target></segment></unit></file></xliff>
            """;
        var offline = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2"><file source-language="en" target-language="de"><body><trans-unit id="t1"><source>Hello</source><target>Hallo</target></trans-unit></body></file></xliff>
            """;
        var replacement = content switch
        {
            "malformed" => "<broken",
            "non-xliff" => "<document>Hello</document>",
            _ => file == "target" ? offline : target,
        };
        if (file == "target")
            target = replacement;
        else
            offline = replacement;

        var exception = Assert.Throws<PluginApplicationException>(() => ApplyProvenance(
            Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "none", "translation", []));
        StringAssert.Contains(exception.Message, expectedError);
    }
}
