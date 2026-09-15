using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Apps.XTM.Actions;
using Apps.XTM.Models.Response.Workflows;

namespace Tests.XTM;

// Local diagnostics: invokes only the static provenance processor; no XTM calls or credentials.
[TestClass]
public class InteroperableSuppliedFilesDebugTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("LocalFileDebug")]
    [DataRow("none", "translation")]
    [DataRow("all", "translation")]
    [DataRow("all", "review")]
    public void SuppliedFiles_MapAllOfflineUnitsAndPreserveTargetContent(string mode, string role)
    {
        var directory = Environment.GetEnvironmentVariable("XTM_DEBUG_XLIFF_DIRECTORY")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var targetPath = Path.Combine(directory, "target.xlf");
        var offlinePath = Path.Combine(directory, "offline-xliff.xlf");
        if (!File.Exists(targetPath) || !File.Exists(offlinePath))
            Assert.Inconclusive("Place the supplied files in Downloads or set XTM_DEBUG_XLIFF_DIRECTORY.");

        var targetBytes = File.ReadAllBytes(targetPath);
        var offlineBytes = File.ReadAllBytes(offlinePath);
        var messages = new List<string>();
        var apply = typeof(InteroperableActions).GetMethod("ApplyProvenanceWithDiagnostics", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate<Func<byte[], byte[], string, string, IReadOnlyList<WorkflowAssignmentBundleResponse>, Action<string>?, byte[]>>();
        var result = apply(targetBytes, offlineBytes, mode, role,
            [new() { From = 1, To = 21, UserId = "42", UserName = "Local reviewer" }], messages.Add);
        var original = XDocument.Parse(Encoding.UTF8.GetString(targetBytes), LoadOptions.PreserveWhitespace);
        var output = XDocument.Parse(Encoding.UTF8.GetString(result), LoadOptions.PreserveWhitespace);
        XNamespace x = "urn:oasis:names:tc:xliff:document:2.0";
        XNamespace its = "http://www.w3.org/2005/11/its";
        Assert.HasCount(740, output.Descendants(x + "segment"));
        var active = output.Descendants(x + "unit")
            .Where(unit => unit.AncestorsAndSelf().Attributes("translate").FirstOrDefault()?.Value != "no").ToArray();
        Assert.HasCount(21, active);
        foreach (var unit in active)
        {
            Assert.AreEqual(mode == "all" ? "Local reviewer (ID 42)" : null,
                unit.Attribute(its + (role == "review" ? "revPerson" : "person"))?.Value);
            StringAssert.StartsWith(unit.Attribute(its + (role == "review" ? "revTool" : "tool"))!.Value, "XTM");
        }

        // Ignore only the attributes this action is allowed to replace on active units.
        var attributes = (role == "review"
            ? new[] { "revPerson", "revPersonRef", "revTool", "revToolRef" }
            : new[] { "person", "personRef", "tool", "toolRef" }).Select(name => its + name).ToArray();
        foreach (var document in new[] { original, output })
            document.Descendants(x + "unit")
                .Where(unit => unit.AncestorsAndSelf().Attributes("translate").FirstOrDefault()?.Value != "no")
                .Attributes().Where(attribute => attributes.Contains(attribute.Name)).Remove();
        Assert.IsTrue(XNode.DeepEquals(original, output), "Only active-unit provenance may change.");
        Assert.HasCount(1, messages);
        StringAssert.Contains(messages[0], "Mapped 21 offline units: 9 exact matches, 12 forgiving matches");
        TestContext.WriteLine(messages[0]);
    }
}
