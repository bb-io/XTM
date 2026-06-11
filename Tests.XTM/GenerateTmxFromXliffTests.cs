using System.Xml.Linq;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request.TranslationMemory;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.XTM.Base;

namespace Tests.XTM;

[TestClass]
public class GenerateTmxFromXliffTests
{
    private static readonly string InputFileName =
        "from_taus_zh_CN_PD_TRUSTED_PARTNERS_16APRIL26_zh_CN_ALL_109_IDs.xlsx.xlf";

    private FileManager _fileManager = null!;
    private string _outputDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _fileManager = new FileManager();

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectDir = Directory.GetParent(baseDir)!.Parent!.Parent!.Parent!.FullName;
        _outputDir = Path.Combine(projectDir, "TestFiles", "Output");
    }

    private TranslationMemoryActions CreateAction() =>
        new(new InvocationContext { AuthenticationCredentialsProviders = [] }, _fileManager);

    [TestMethod]
    public async Task GenerateTmxFromXliff_DefaultTagging_SegmentCountIsCorrect()
    {
        var action = CreateAction();
        var result = await action.GenerateTmxFromXliff(new GenerateTmxFromXliffRequest
        {
            File = new FileReference { Name = InputFileName }
        });

        Assert.AreEqual(65, result.SegmentsTagged, "Expected 65 segments with score >= threshold");
        Assert.AreEqual("QE", result.TagGroupUsed);
        Assert.AreEqual("auto_approved", result.TagUsed);
        Assert.IsNotNull(result.File);
    }

    [TestMethod]
    public async Task GenerateTmxFromXliff_DefaultTagging_TmxIsValidAndWellFormed()
    {
        var action = CreateAction();
        var result = await action.GenerateTmxFromXliff(new GenerateTmxFromXliffRequest
        {
            File = new FileReference { Name = InputFileName }
        });

        var outputPath = Path.Combine(_outputDir, result.File.Name);
        Assert.IsTrue(File.Exists(outputPath), $"TMX file not found at: {outputPath}");

        var tmx = XDocument.Load(outputPath);

        Assert.AreEqual("tmx", tmx.Root!.Name.LocalName);
        Assert.AreEqual("1.4", tmx.Root.Attribute("version")?.Value);

        var header = tmx.Root.Element("header");
        Assert.IsNotNull(header, "<header> element missing");
        Assert.AreEqual("en-GB", header.Attribute("srclang")?.Value, "srclang must match XLIFF srcLang");

        var body = tmx.Root.Element("body");
        Assert.IsNotNull(body, "<body> element missing");

        var tuList = body.Elements("tu").ToList();
        Assert.HasCount(65, tuList, "TMX must contain exactly 65 <tu> elements");
    }

    [TestMethod]
    public async Task GenerateTmxFromXliff_DefaultTagging_EachTuHasCorrectPropAndBothLanguages()
    {
        var action = CreateAction();
        var result = await action.GenerateTmxFromXliff(new GenerateTmxFromXliffRequest
        {
            File = new FileReference { Name = InputFileName }
        });

        var outputPath = Path.Combine(_outputDir, result.File.Name);
        var tmx = XDocument.Load(outputPath);
        var tuList = tmx.Root!.Element("body")!.Elements("tu").ToList();

        foreach (var tu in tuList)
        {
            var prop = tu.Element("prop");
            Assert.IsNotNull(prop, "<prop> missing from <tu>");
            Assert.AreEqual("QE", prop.Attribute("type")?.Value, "<prop type> must equal tag group");
            Assert.AreEqual("auto_approved", prop.Value, "<prop> content must equal tag");

            var tuvs = tu.Elements("tuv").ToList();
            Assert.HasCount(2, tuvs, "Each <tu> must have exactly 2 <tuv> elements");

            var langs = tuvs.Select(t => t.Attribute(XNamespace.Xml + "lang")?.Value).ToList();
            CollectionAssert.Contains(langs, "en-GB", "Source TUV must be en-GB");
            CollectionAssert.Contains(langs, "zh-CN", "Target TUV must be zh-CN");

            foreach (var tuv in tuvs)
            {
                var seg = tuv.Element("seg");
                Assert.IsNotNull(seg, "<seg> missing from <tuv>");
                Assert.IsFalse(string.IsNullOrWhiteSpace(seg.Value), "<seg> must not be empty");
            }
        }
    }

    [TestMethod]
    public async Task GenerateTmxFromXliff_CustomTagging_UsesProvidedValues()
    {
        var action = CreateAction();
        var result = await action.GenerateTmxFromXliff(new GenerateTmxFromXliffRequest
        {
            File = new FileReference { Name = InputFileName },
            Tag = "my_tag",
            TagGroup = "MY_GROUP"
        });

        Assert.AreEqual("MY_GROUP", result.TagGroupUsed);
        Assert.AreEqual("my_tag", result.TagUsed);

        var outputPath = Path.Combine(_outputDir, result.File.Name);
        var tmx = XDocument.Load(outputPath);
        var firstProp = tmx.Root!.Element("body")!.Element("tu")!.Element("prop")!;
        Assert.AreEqual("MY_GROUP", firstProp.Attribute("type")?.Value);
        Assert.AreEqual("my_tag", firstProp.Value);
    }
}
