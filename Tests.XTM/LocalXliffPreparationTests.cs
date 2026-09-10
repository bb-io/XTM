using Apps.XTM.Utils;

namespace Tests.XTM;

[TestClass]
public class LocalXliffPreparationTests
{
    public TestContext TestContext { get; set; } = null!;

    // PowerShell (from the repository root):
    // $env:XTM_PREPARE_LOCAL_XLIFF = '1'
    // dotnet test Tests.XTM/Tests.XTM.csproj --filter FullyQualifiedName~LocalXliffPreparationTests --logger "console;verbosity=detailed"
    [TestMethod]
    [TestCategory("LocalBatch")]
    public async Task Prepare_BlacklakeXliffFiles_ToLocalProcessedFolder()
    {
        if (Environment.GetEnvironmentVariable("XTM_PREPARE_LOCAL_XLIFF") != "1")
            Assert.Inconclusive("Set XTM_PREPARE_LOCAL_XLIFF=1 to run this local batch explicitly.");

        const string inputRoot = @"C:\coding\blackbird-xtm\Blacklake for ID";
        const string outputRoot = @"C:\coding\blackbird-xtm\processed";
        Assert.IsTrue(Directory.Exists(inputRoot), $"Input folder does not exist: {inputRoot}");

        var files = Directory.EnumerateFiles(inputRoot, "*", SearchOption.AllDirectories)
            .Where(path => string.Equals(Path.GetExtension(path), ".xlf", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.IsNotEmpty(files, "No XLIFF files found.");

        string[] excludedStates = ["translated", "reviewed", "final"];
        var failures = new List<string>();
        var succeeded = 0;
        long total = 0, excluded = 0, left = 0, words = 0;

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(inputRoot, file);
            try
            {
                var content = await File.ReadAllBytesAsync(file);
                // This is the same preparation method used by UploadSelectedSourceXliff.
                // XLIFF supplies its own source language; no project lookup is needed.
                var prepared = XliffSourceSelection.Prepare(content, excludedStates,
                    Path.GetFileName(file), "application/xliff+xml");
                var outputPath = Path.Combine(outputRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                await File.WriteAllBytesAsync(outputPath, prepared.Content);

                succeeded++;
                total += prepared.SegmentsTotal;
                excluded += prepared.SegmentsExcluded;
                left += prepared.SegmentsLeft;
                words += prepared.ApproximateWordCount;
                TestContext.WriteLine($"OK {relativePath}: total={prepared.SegmentsTotal}, " +
                    $"excluded={prepared.SegmentsExcluded}, left={prepared.SegmentsLeft}, " +
                    $"approximate words={prepared.ApproximateWordCount}");
            }
            catch (Exception exception)
            {
                var failure = $"{relativePath}: {exception.Message}";
                failures.Add(failure);
                TestContext.WriteLine($"FAILED {failure}");
            }
        }

        TestContext.WriteLine($"Files: {files.Length}; saved: {succeeded}; failed: {failures.Count}. " +
            $"Segments (saved files): total={total}, excluded={excluded}, left={left}; approximate words={words}.");
        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        Assert.AreEqual(files.Length, succeeded);
        foreach (var file in files)
            Assert.IsTrue(File.Exists(Path.Combine(outputRoot, Path.GetRelativePath(inputRoot, file))),
                $"Missing output for {file}");
    }
}
