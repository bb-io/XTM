using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Apps.XTM.Actions;
using Apps.XTM.Models.Response.Workflows;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.XTM;

[TestClass]
public class ForgivingProvenanceTests
{
    private static readonly Func<byte[], byte[], string, string, IReadOnlyList<WorkflowAssignmentBundleResponse>, Action<string>?, byte[]> Apply =
        typeof(InteroperableActions).GetMethod("ApplyProvenanceWithDiagnostics", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate<Func<byte[], byte[], string, string, IReadOnlyList<WorkflowAssignmentBundleResponse>, Action<string>?, byte[]>>();

    [TestMethod]
    [DataRow("&amp;lt;a href=\"{0}\"&amp;gt;Help&amp;lt;/a&amp;gt;", "<x id=\"1\"/>Help<x id=\"2\"/>", true)]
    [DataRow("&amp;lt;a {0}&amp;gt;Help&amp;lt;/a&amp;gt;", "<x id=\"1\"/>Help<x id=\"2\"/>", true)]
    [DataRow("&lt;a title=\"a &gt; b\"&gt;Help&lt;/a&gt;", "<x id=\"1\"/>Help<x id=\"2\"/>", true)]
    [DataRow("<pc id=\"a\">Help</pc>", "<bpt id=\"1\">&lt;b&gt;</bpt>Help<ept id=\"1\">&lt;/b&gt;</ept>", true)]
    [DataRow("A &amp;amp; B", "A &amp; B", true)]
    [DataRow("A &amp;nbsp; B", " A  B ", true)]
    [DataRow("A &amp;amp;amp; B", "A &amp; B", false)]
    [DataRow("&lt;a&gt;Help&lt;/a&gt;", "<x id=\"1\"/>Support<x id=\"2\"/>", false)]
    [DataRow("a &lt; b &gt; c", "a c", false)]
    [DataRow("&lt;b&gt;&lt;/b&gt;", "<x id=\"1\"/>", false)]
    public void Apply_NormalizesSourceRepresentationWithoutChangingContent(string source, string offlineSource, bool succeeds)
    {
        var target = Target($"<unit id=\"u\"><segment><source xml:space=\"preserve\">{source}</source><target>Original &amp;lt;br&amp;gt; target</target></segment></unit>");
        var offline = Offline($"<trans-unit id=\"t1\"><source xml:space=\"preserve\">{offlineSource}</source><target>Different target</target></trans-unit>");
        var messages = new List<string>();
        byte[] Run() => Apply(Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "all", "translation",
            [new() { From = 1, To = 1, UserId = "1", UserName = "Translator" }], messages.Add);
        if (!succeeds)
        {
            var exception = Assert.Throws<PluginApplicationException>(() => Run());
            StringAssert.Contains(exception.Message, "target unit 'u', offline unit 't1'");
            StringAssert.Contains(exception.Message, "Source text is missing or differs after forgiving normalization");
            Assert.IsEmpty(messages);
            return;
        }
        var output = XDocument.Parse(Encoding.UTF8.GetString(Run()), LoadOptions.PreserveWhitespace);
        XNamespace x = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace its = "http://www.w3.org/2005/11/its";
        Assert.AreEqual("Translator (ID 1)", output.Descendants(x + "unit").Single().Attribute(its + "person")?.Value);
        output.Descendants(x + "unit").Attributes().Where(a => a.Name.Namespace == its).Remove();
        Assert.IsTrue(XNode.DeepEquals(XDocument.Parse(target, LoadOptions.PreserveWhitespace), output));
        Assert.HasCount(1, messages);
        StringAssert.Contains(messages[0], "0 exact matches, 1 forgiving matches");
        StringAssert.Contains(messages[0], "1 offline units mapped to segments with target differences");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Apply_Splits_RequiresUniqueCompleteAlignment(bool ambiguous)
    {
        // The first source can consume A alone (forgiving) or A + the code (exact).
        // Only the remaining source decides whether both complete paths are valid.
        var target = Target($"""
            <unit id="first"><segment><source>A<ph id="p"/></source><target>Erste</target></segment></unit>
            <unit id="second"><segment><source>{(ambiguous ? "<ph id=\"p\"/>" : "")}B</source><target>Zweite</target></segment></unit>
            """);
        var offline = Offline("""
            <group id="g">
              <trans-unit id="t1"><source>A</source><target state="signed-off">A</target></trans-unit>
              <trans-unit id="t2"><source><ph id="p"/></source><target state="signed-off"><ph id="p"/></target></trans-unit>
              <trans-unit id="t3"><source>B</source><target state="signed-off">B</target></trans-unit>
            </group>
            """);
        byte[] Run() => Apply(Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "only_confirmed", "translation",
            [new() { From = 1, To = 2, UserId = "1", UserName = "First" },
             new() { From = 3, To = 3, UserId = "2", UserName = "Second" }], null);
        if (ambiguous)
        {
            var exception = Assert.Throws<PluginApplicationException>(() => Run());
            StringAssert.Contains(exception.Message, "multiple complete source alignments");
            return;
        }
        var output = XDocument.Parse(Encoding.UTF8.GetString(Run()));
        XNamespace x = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var units = output.Descendants(x + "unit").ToArray();
        Assert.AreEqual("First (ID 1)", units[0].Attribute(its + "person")?.Value);
        Assert.AreEqual("Second (ID 2)", units[1].Attribute(its + "person")?.Value);
    }

    [TestMethod]
    public void Apply_RepeatedNormalizedSources_PairsByOrder()
    {
        var target = Target("""
            <unit id="first"><segment><source>&amp;lt;b&amp;gt;Help&amp;lt;/b&amp;gt;</source><target>One</target></segment></unit>
            <unit id="second"><segment><source>&amp;lt;i&amp;gt;Help&amp;lt;/i&amp;gt;</source><target>Two</target></segment></unit>
            """);
        var offline = Offline("""
            <trans-unit id="t9"><source><x id="1"/>Help<x id="2"/></source><target>Two</target></trans-unit>
            <trans-unit id="t3"><source><x id="3"/>Help<x id="4"/></source><target>One</target></trans-unit>
            """);
        var output = XDocument.Parse(Encoding.UTF8.GetString(Apply(Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "all", "review",
            [new() { From = 9, To = 9, UserId = "9", UserName = "First" },
             new() { From = 3, To = 3, UserId = "3", UserName = "Second" }], null)));
        XNamespace x = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace its = "http://www.w3.org/2005/11/its";
        var units = output.Descendants(x + "unit").ToArray();
        Assert.AreEqual("First (ID 9)", units[0].Attribute(its + "revPerson")?.Value);
        Assert.AreEqual("Second (ID 3)", units[1].Attribute(its + "revPerson")?.Value);
    }

    private static string Target(string units) => $"""
        <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:its="http://www.w3.org/2005/11/its" version="2.1" srcLang="en" trgLang="de"><file id="f">{units}</file></xliff>
        """;

    private static string Offline(string units) => $"""
        <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" version="1.2"><file source-language="en" target-language="de"><body>{units}</body></file></xliff>
        """;
}
