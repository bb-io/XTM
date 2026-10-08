using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using Apps.XTM.Actions;
using Apps.XTM.Constants;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Response.Workflows;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Moq;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Transformations;

namespace Tests.XTM;

[TestClass]
public class InteroperableSegmentStatusTests
{
    private static readonly XNamespace Mapping = "https://blackbird.io/xliff/xtm-segment-mapping";
    private static readonly XNamespace Native = "urn:oasis:names:tc:xliff:document:1.2";

    [TestMethod]
    [DataRow("2.1", "urn:oasis:names:tc:xliff:document:2.0")]
    [DataRow("2.2", "urn:oasis:names:tc:xliff:document:2.2")]
    public async Task Copy_UsesScopedIdsAndIdlessPosition_PreservesNativeContent(string version, string namespaceName)
    {
        var target = $"""
            <xliff xmlns="{namespaceName}" version="{version}" srcLang="en" trgLang="de">
              <file id="f1"><unit id="same">
                <segment id="s5" state="final"><source>Five</source><target>Reviewed edit must not replace XTM text</target></segment>
                <segment id="s4" state="reviewed"><source>Four</source><target>Vier</target></segment>
                <segment id="s3" state="translated"><source>Three</source><target>Drei</target></segment>
                <segment id="s2" state="initial"><source>Two</source><target>Zwei</target></segment>
                <segment id="s1"><source>One</source><target>Eins</target></segment>
              </unit><unit id="excluded" translate="no"><segment state="final"><source>Excluded</source><target>Ausgeschlossen</target></segment></unit></file>
              <file id="f2"><unit id="same"><ignorable id="space"><source> </source><target> </target></ignorable><segment state="reviewed"><source>Idless</source><target>Ohne ID</target></segment></unit></file>
            </xliff>
            """;
        var translation = XDocument.Parse("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:xtm="urn:xliff-xtm-extensions" version="1.2">
              <file original="native-file-identifier" source-language="en" target-language="de" xtm:xliff-identifier="native"><header><note>Native header</note></header><body><group id="g1" xml:space="preserve"/></body></file>
            </xliff>
            """);
        var group = translation.Descendants(Native + "group").Single();
        for (var index = 1; index <= 7; index++)
        {
            // t6 exercises idless position; t7 is another offline split of s5.
            var unit = new XElement(Native + "trans-unit",
                new XAttribute("id", $"t{index}"),
                new XAttribute(XNamespace.Get("urn:xliff-xtm-extensions") + "x-previous-crc", $"crc-{index}"),
                new XAttribute(Mapping + "file-id", index == 6 ? "f2" : "f1"),
                new XAttribute(Mapping + "unit-id", "same"),
                new XAttribute(Mapping + "segment-index", index == 6 ? 1 : index == 7 ? 5 : index),
                new XElement(Native + "source", $"Source {index}"),
                new XElement(Native + "target", new XAttribute("state", "translated"), new XAttribute("state-qualifier", "fuzzy-match"),
                    $"  Native & translation {index} ", new XElement(Native + "x", new XAttribute("id", "1")), "  "),
                new XElement(Native + "alt-trans", new XElement(Native + "target", "Suggestion")));
            if (index != 6)
                unit.SetAttributeValue(Mapping + "segment-id", $"s{(index == 7 ? 5 : index)}");
            group.Add(unit);
        }
        group.Add(XElement.Parse("""
            <trans-unit xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:map="https://blackbird.io/xliff/xtm-segment-mapping" xmlns:bb="http://blackbird.io/" id="t8" map:file-id="f2" map:unit-id="same" map:segment-index="1">
              <source>First.Second.</source>
              <seg-source><mrk mtype="seg" mid="1">First.</mrk><mrk mtype="seg" mid="2">Second.</mrk></seg-source>
              <target><mrk mtype="seg" mid="1" bb:customState="new">Erste.</mrk><mrk mtype="seg" mid="2" bb:customState="final">Zweite.</mrk></target>
            </trans-unit>
            """));
        using var originalStream = new MemoryStream(Encoding.UTF8.GetBytes(translation.ToString(SaveOptions.DisableFormatting)));
        var originalLoad = Transformation.Load(originalStream, "native.xlf");
        Assert.IsTrue(originalLoad.Success, originalLoad.Error);
        var original = XDocument.Parse(Xliff1Serializer.Serialize(originalLoad.Value), LoadOptions.PreserveWhitespace);
        byte[]? output = null;
        var manager = new Mock<IFileManagementClient>(MockBehavior.Strict);
        manager.Setup(x => x.DownloadAsync(It.IsAny<FileReference>()))
            .Returns((FileReference file) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(
                file.Name == "native.xlf" ? translation.ToString(SaveOptions.DisableFormatting) : target))));
        manager.Setup(x => x.UploadAsync(It.IsAny<Stream>(), "application/xliff+xml", "native.xlf"))
            .Returns(async (Stream stream, string contentType, string name) =>
            {
                using var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                output = copy.ToArray();
                return new FileReference { Name = name, ContentType = contentType };
            });
        var result = await new InteroperableActions(new InvocationContext(), manager.Object)
            .CopySegmentStatusesToXtmXliff(new()
            {
                TranslationFile = new() { Name = "native.xlf" },
                TargetFile = new() { Name = "reviewed.xlf" },
            });
        Assert.AreEqual("native.xlf", result.File.Name);
        var updated = XDocument.Parse(Encoding.UTF8.GetString(output!), LoadOptions.PreserveWhitespace);
        CollectionAssert.AreEqual(new string?[] { null, "new", "translated", "signed-off", "final", "signed-off", "final", "signed-off" },
            updated.Descendants(Native + "trans-unit").Select(unit => (string?)unit.Element(Native + "target")!.Attribute("state")).ToArray());
        using var updatedStream = new MemoryStream(output!);
        var updatedLoad = Transformation.Load(updatedStream, "native.xlf");
        Assert.IsTrue(updatedLoad.Success, updatedLoad.Error);
        var nativeSegments = updatedLoad.Value.GetUnits().Last().Segments;
        Assert.HasCount(2, nativeSegments);
        Assert.IsTrue(nativeSegments.All(segment => segment.State == Blackbird.Filters.Enums.SegmentState.Reviewed),
            "Every native segment must receive the mapped state, including existing per-marker state overrides.");
        foreach (var document in new[] { original, updated })
        {
            document.Descendants(Native + "trans-unit").Elements(Native + "target").Attributes("state").Remove();
            document.Descendants(Native + "mrk").Attributes(XNamespace.Get("http://blackbird.io/") + "customState").Remove();
            document.Descendants(Native + "mrk").Attributes().Where(attribute => attribute.IsNamespaceDeclaration
                && attribute.Value == "http://blackbird.io/").Remove();
        }
        Assert.IsTrue(XNode.DeepEquals(original, updated), "After Filters serialization, only target state attributes may differ, including with reordered segments and split mappings.");
    }

    [TestMethod]
    [DataRow("missing-mapping", "missing valid segment mapping")]
    [DataRow("missing-file", "maps to 0 target units")]
    [DataRow("missing-unit", "maps to 0 target units")]
    [DataRow("duplicate-unit", "maps to 2 target units")]
    [DataRow("duplicate-file", "maps to 2 target units")]
    [DataRow("missing-segment", "exactly one target segment")]
    [DataRow("duplicate-segment", "exactly one target segment")]
    [DataRow("zero-position", "missing valid segment mapping")]
    [DataRow("invalid-position", "missing valid segment mapping")]
    [DataRow("idless-position-outside", "exactly one target segment")]
    [DataRow("idless-now-has-id", "exactly one target segment")]
    [DataRow("unsupported-state", "unsupported state 'approved'")]
    [DataRow("missing-target", "exactly one target element")]
    [DataRow("duplicate-target", "exactly one target element")]
    [DataRow("empty-translation", "does not contain any translation units")]
    [DataRow("wrong-translation-version", "must be mapped XTM XLIFF 1.2")]
    [DataRow("wrong-target-version", "must be a full interoperable XLIFF 2 file")]
    [DataRow("malformed-translation", "Provide valid XLIFF files")]
    [DataRow("malformed-target", "Provide valid XLIFF files")]
    public async Task Copy_RejectsUnsafeInputs_WithoutUploadingPartialResult(string scenario, string expectedError)
    {
        var target = XDocument.Parse("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1"><file id="f"><unit id="u"><segment id="s" state="final"><source>Hello</source><target>Hallo</target></segment></unit></file></xliff>
            """);
        var translation = XDocument.Parse("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:bb="https://blackbird.io/xliff/xtm-segment-mapping" version="1.2"><file original="native"><body><trans-unit id="t1" bb:file-id="f" bb:unit-id="u" bb:segment-id="s" bb:segment-index="1"><source>Hello</source><target state="translated">Hallo</target></trans-unit></body></file></xliff>
            """);
        var unit = translation.Descendants(Native + "trans-unit").Single();
        var targetUnit = target.Descendants().Single(e => e.Name.LocalName == "unit");
        var segment = targetUnit.Elements().Single();
        switch (scenario)
        {
            case "missing-mapping": unit.Attribute(Mapping + "unit-id")!.Remove(); break;
            case "missing-file": unit.SetAttributeValue(Mapping + "file-id", "other"); break;
            case "missing-unit": unit.SetAttributeValue(Mapping + "unit-id", "other"); break;
            case "duplicate-unit": targetUnit.AddAfterSelf(new XElement(targetUnit)); break;
            case "duplicate-file": targetUnit.Parent!.AddAfterSelf(new XElement(targetUnit.Parent)); break;
            case "missing-segment": unit.SetAttributeValue(Mapping + "segment-id", "other"); break;
            case "duplicate-segment": segment.AddAfterSelf(new XElement(segment)); break;
            case "zero-position": unit.SetAttributeValue(Mapping + "segment-index", "0"); break;
            case "invalid-position": unit.SetAttributeValue(Mapping + "segment-index", "invalid"); break;
            case "idless-position-outside":
                unit.Attribute(Mapping + "segment-id")!.Remove();
                unit.SetAttributeValue(Mapping + "segment-index", "2");
                break;
            case "idless-now-has-id": unit.Attribute(Mapping + "segment-id")!.Remove(); break;
            case "unsupported-state": segment.SetAttributeValue("state", "approved"); break;
            case "missing-target": unit.Element(Native + "target")!.Remove(); break;
            case "duplicate-target": unit.Add(new XElement(unit.Element(Native + "target")!)); break;
            case "empty-translation": unit.Remove(); break;
            case "wrong-translation-version": translation.Root!.SetAttributeValue("version", "2.1"); break;
            case "wrong-target-version": target.Root!.SetAttributeValue("version", "1.2"); break;
        }
        // Make a valid first unit precede the invalid unit, so validation failures cannot emit a partial result.
        if (scenario is not ("empty-translation" or "wrong-translation-version"))
        {
            var valid = XDocument.Parse("""
                <trans-unit xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:bb="https://blackbird.io/xliff/xtm-segment-mapping" id="control" bb:file-id="control" bb:unit-id="control" bb:segment-id="s" bb:segment-index="1"><source>Control</source><target>Kontrolle</target></trans-unit>
                """).Root!;
            unit.AddBeforeSelf(valid);
            target.Root!.Add(new XElement(target.Root.Name.Namespace + "file", new XAttribute("id", "control"),
                new XElement(target.Root.Name.Namespace + "unit", new XAttribute("id", "control"),
                    new XElement(segment.Name, new XAttribute("id", "s"), new XAttribute("state", "reviewed"),
                        new XElement(segment.Name.Namespace + "source", "Control"), new XElement(segment.Name.Namespace + "target", "Kontrolle")))));
        }
        var manager = new Mock<IFileManagementClient>(MockBehavior.Strict);
        manager.Setup(x => x.DownloadAsync(It.IsAny<FileReference>()))
            .Returns((FileReference file) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(
                scenario == $"malformed-{(file.Name == "translation.xlf" ? "translation" : "target")}" ? "<invalid" :
                    (file.Name == "translation.xlf" ? translation : target).ToString()))));
        var error = await Assert.ThrowsAsync<PluginMisconfigurationException>(() =>
            new InteroperableActions(new InvocationContext(), manager.Object).CopySegmentStatusesToXtmXliff(new()
            {
                TranslationFile = new() { Name = "translation.xlf" },
                TargetFile = new() { Name = "target.xlf" },
            }));
        StringAssert.Contains(error.Message, expectedError);
        manager.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task Upload_MappedNativeXliff_LocksSelectedStatesAndPreservesUploadIdentifiers()
    {
        var original = XDocument.Parse("""
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:xtm="urn:xliff-xtm-extensions" xmlns:bb="https://blackbird.io/xliff/xtm-segment-mapping" version="1.2"><file original="native-upload-identifier" source-language="en" target-language="de" xtm:xliff-identifier="native"><body><group id="g1"/></body></file></xliff>
            """);
        var states = new string?[] { null, "new", "translated", "signed-off", "final" };
        var group = original.Descendants(Native + "group").Single();
        for (var index = 0; index < states.Length; index++)
        {
            var unit = new XElement(Native + "trans-unit", new XAttribute("id", $"t{index + 1}"),
                new XAttribute(XNamespace.Get("urn:xliff-xtm-extensions") + "x-previous-crc", $"crc-{index}"),
                new XAttribute(Mapping + "unit-id", $"original-{index}"),
                new XElement(Native + "source", $"Source {index}"), new XElement(Native + "target", $"Target {index}"));
            unit.Element(Native + "target")!.SetAttributeValue("state", states[index]);
            group.Add(unit);
        }
        var manager = new Mock<IFileManagementClient>(MockBehavior.Strict);
        manager.Setup(x => x.DownloadAsync(It.IsAny<FileReference>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(original.ToString()))));
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using var listener = new HttpListener();
        var url = $"http://127.0.0.1:{port}";
        listener.Prefixes.Add(url + "/");
        listener.Start();
        XDocument? received = null;
        var server = Task.Run(async () =>
        {
            for (var requestIndex = 0; requestIndex < 2; requestIndex++)
            {
                var request = await listener.GetContextAsync();
                string response;
                if (requestIndex == 0)
                {
                    Assert.AreEqual("POST", request.Request.HttpMethod);
                    Assert.AreEqual("/projects/123/files/translations/upload", request.Request.Url!.AbsolutePath);
                    using var reader = new StreamReader(request.Request.InputStream);
                    var body = await reader.ReadToEndAsync();
                    var start = body.IndexOf("<xliff", StringComparison.Ordinal);
                    var end = body.IndexOf("</xliff>", start, StringComparison.Ordinal) + "</xliff>".Length;
                    received = XDocument.Parse(body[start..end]);
                    StringAssert.Contains(body, "ACCORDINGLY_TO_STATE");
                    response = """{"file":{"fileId":"91","jobId":"42"}}""";
                }
                else
                {
                    Assert.AreEqual("/projects/123/files/translations/91/status", request.Request.Url!.AbsolutePath);
                    response = """{"status":"FINISHED"}""";
                }
                request.Response.ContentType = "application/json";
                await request.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(response));
                request.Response.Close();
            }
        });
        var context = new InvocationContext
        {
            AuthenticationCredentialsProviders =
            [
                new AuthenticationCredentialsProvider(CredsNames.ConnectionType, ConnectionTypes.GeneratedToken),
                new AuthenticationCredentialsProvider(CredsNames.Url, url),
                new AuthenticationCredentialsProvider(CredsNames.Token, "local-test-token"),
            ],
        };
        try
        {
            var result = await new FileActions(context, manager.Object).UploadTranslationFile(new() { ProjectId = "123" },
                new() { JobId = "42", FileType = "XLIFF", File = new() { Name = "mapped.xlf" }, SegmentStatusApproving = "ACCORDINGLY_TO_STATE" },
                new() { LockSegmentByStates = ["reviewed", "final"] }).WaitAsync(TimeSpan.FromSeconds(20));
            await server.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.AreEqual("FINISHED", result.Status);
            Assert.AreEqual("native-upload-identifier", received!.Descendants(Native + "file").Single().Attribute("original")?.Value);
            var units = received.Descendants(Native + "trans-unit").ToArray();
            Assert.HasCount(5, units);
            for (var index = 0; index < units.Length; index++)
            {
                Assert.AreEqual($"t{index + 1}", units[index].Attribute("id")?.Value);
                Assert.AreEqual($"crc-{index}", units[index].Attribute(XNamespace.Get("urn:xliff-xtm-extensions") + "x-previous-crc")?.Value);
                Assert.AreEqual($"original-{index}", units[index].Attribute(Mapping + "unit-id")?.Value);
                Assert.AreEqual($"Target {index}", units[index].Element(Native + "target")!.Value);
                Assert.AreEqual(states[index], (string?)units[index].Element(Native + "target")!.Attribute("state"));
                Assert.AreEqual(index >= 3 ? "yes" : null, units[index].Attribute(XNamespace.Get("urn:xliff-xtm-extensions") + "locked")?.Value);
            }
        }
        finally
        {
            listener.Stop();
            try { await server; }
            catch (HttpListenerException) when (!listener.IsListening) { }
            catch (ObjectDisposedException) when (!listener.IsListening) { }
        }
    }

    [TestMethod]
    public async Task Copy_SuppliedInteroperableSample_UsesOriginalKeysAndIdlessSegments()
    {
        var sample = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestFiles", "Input", "sample-interoperable.xliff"));
        var sampleNamespace = sample.Root!.Name.Namespace;
        var sampleUnits = sample.Descendants(sampleNamespace + "unit").ToArray();
        Assert.HasCount(2, sampleUnits);
        var offline = new XDocument(new XElement(Native + "xliff", new XAttribute("version", "1.2"),
            new XElement(Native + "file", new XAttribute("original", "xtm-native"), new XElement(Native + "body"))));
        for (var index = 0; index < sampleUnits.Length; index++)
        {
            sampleUnits[index].Element(sampleNamespace + "segment")!.SetAttributeValue("state", index == 0 ? "reviewed" : "final");
            offline.Descendants(Native + "body").Single().Add(new XElement(Native + "trans-unit", new XAttribute("id", $"t{index + 1}"),
                new XAttribute(Mapping + "file-id", "f1"), new XAttribute(Mapping + "unit-id", sampleUnits[index].Attribute("id")!.Value),
                new XAttribute(Mapping + "segment-index", "1"), new XElement(Native + "source", $"Native source {index}"),
                new XElement(Native + "target", $"Native translation {index}")));
        }
        var manager = new Mock<IFileManagementClient>(MockBehavior.Strict);
        manager.Setup(x => x.DownloadAsync(It.IsAny<FileReference>()))
            .Returns((FileReference file) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(
                (file.Name == "native.xlf" ? offline : sample).ToString()))));
        byte[]? output = null;
        manager.Setup(x => x.UploadAsync(It.IsAny<Stream>(), "application/xliff+xml", "native.xlf"))
            .Returns(async (Stream stream, string type, string name) =>
            {
                using var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                output = copy.ToArray();
                return new FileReference { Name = name, ContentType = type };
            });
        await new InteroperableActions(new InvocationContext(), manager.Object).CopySegmentStatusesToXtmXliff(new()
        {
            TranslationFile = new() { Name = "native.xlf" },
            TargetFile = new() { Name = "sample-interoperable.xliff" },
        });
        var updated = XDocument.Parse(Encoding.UTF8.GetString(output!));
        CollectionAssert.AreEqual(new[] { "signed-off", "final" }, updated.Descendants(Native + "target").Select(t => t.Attribute("state")!.Value).ToArray());
        CollectionAssert.AreEqual(sampleUnits.Select(u => u.Attribute("id")!.Value).ToArray(),
            updated.Descendants(Native + "trans-unit").Select(u => u.Attribute(Mapping + "unit-id")!.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "Native translation 0", "Native translation 1" },
            updated.Descendants(Native + "target").Select(t => t.Value).ToArray());
        manager.Verify(x => x.UploadAsync(It.IsAny<Stream>(), "application/xliff+xml", "native.xlf"), Times.Once);
    }

    [TestMethod]
    public void DownloadMapping_ReferencesSplitSegmentsAndScopes_LeavesExcludedContentIntact()
    {
        const string target = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.1"><file id="first">
              <unit id="same"><segment><source>First. Second.</source><target>Erste. Zweite.</target></segment></unit>
              <unit id="excluded" translate="no"><segment state="final"><source>Excluded.</source><target>Ausgeschlossen.</target></segment></unit>
            </file><file id="second"><unit id="same"><segment id="s"><source>Third.</source><target>Dritte.</target></segment></unit></file></xliff>
            """;
        const string offline = """
            <xliff xmlns="urn:oasis:names:tc:xliff:document:1.2" xmlns:xtm="urn:xliff-xtm-extensions" version="1.2"><file original="native" source-language="en" target-language="de"><body>
              <group id="g1"><trans-unit id="t1" xtm:x-next-crc="crc"><source>First.</source><target>Erste.</target></trans-unit><trans-unit id="t2"><source>Second.</source><target>Zweite.</target></trans-unit></group>
              <group id="g2"><trans-unit id="t3"><source>Third.</source><target>Dritte.</target></trans-unit></group>
            </body></file></xliff>
            """;
        var apply = typeof(InteroperableActions).GetMethod("ApplyProvenanceWithDiagnostics", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate<Func<byte[], byte[], string, string, IReadOnlyList<WorkflowAssignmentBundleResponse>, Action<string>?, (byte[] Target, byte[] Translation)>>();
        var output = apply(Encoding.UTF8.GetBytes(target), Encoding.UTF8.GetBytes(offline), "none", "translation", [], null);
        var mapped = XDocument.Parse(Encoding.UTF8.GetString(output.Translation));
        var units = mapped.Descendants(Native + "trans-unit").ToArray();
        CollectionAssert.AreEqual(new[] { "first", "first", "second" }, units.Select(u => u.Attribute(Mapping + "file-id")!.Value).ToArray());
        Assert.IsTrue(units.All(u => u.Attribute(Mapping + "unit-id")!.Value == "same"));
        Assert.IsTrue(units.All(u => u.Attribute(Mapping + "segment-index")!.Value == "1"));
        Assert.IsNull(units[0].Attribute(Mapping + "segment-id"));
        Assert.IsNull(units[1].Attribute(Mapping + "segment-id"));
        Assert.AreEqual("s", units[2].Attribute(Mapping + "segment-id")!.Value);
        foreach (var u in units)
            u.Attributes().Where(a => a.Name.Namespace == Mapping).Remove();
        mapped.Root!.DescendantsAndSelf().Attributes().Where(a => a.IsNamespaceDeclaration && a.Value == Mapping.NamespaceName).Remove();
        Assert.IsTrue(XNode.DeepEquals(XDocument.Parse(offline), mapped));
        var full = XDocument.Parse(Encoding.UTF8.GetString(output.Target));
        var excluded = full.Descendants().Single(e => e.Name.LocalName == "unit" && (string?)e.Attribute("id") == "excluded");
        Assert.IsTrue(XNode.DeepEquals(XDocument.Parse(target).Descendants().Single(e => e.Name.LocalName == "unit" && (string?)e.Attribute("id") == "excluded"), excluded));
    }
}
