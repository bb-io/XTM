using Apps.XTM.Actions;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTMTests.Base;

namespace Tests.XTM
{
    [TestClass]
    public class FileActionsTests: TestBase
    {
        private readonly FileActions _fileActions;

        public FileActionsTests()
        {
            _fileActions = new FileActions(InvocationContext, FileManager);
        }

        [TestMethod]
        public async Task GenerateFiles_IsSuccess()
        {
            var project = new ProjectRequest { ProjectId = "108698822" };
            var fileGenerate = new GenerateFileRequest { FileType = "XLIFF" };

            var response = await _fileActions.GenerateFiles(project, fileGenerate);

            Console.WriteLine($"Total files generated: {response.Files.Length}");
            foreach (var job in response.Files)
            {
                Console.WriteLine($"{job.FileId} - {job.FileType}");
            }   
        }


        [TestMethod]
        public async Task UploadSourceFile_successful()
        {
            var projectRequest = new ProjectRequest { ProjectId = "6883" };
            var fileRequest = new UploadSourceFileRequest
            {
                File = new FileReference { Name = "sample.txt", ContentType = "text/plain" },
                WorkflowId = "6290",
            };

            var response = await _fileActions.UploadSourceFile(
                projectRequest,
                fileRequest);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(json);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public async Task UploadSourceFile_MoreThan50_successful()
        {
            var projectRequest = new ProjectRequest { ProjectId = "28090" };

            for (int i = 1; i <= 100; i++)
            {
                var fileRequest = new UploadSourceFileRequest
                {
                    File = await FileManager.UploadTestFileAsync("sample.txt"),
                    WorkflowId = "6430",
                    Name = $"sample_{i:D3}.txt",
                };
                await _fileActions.UploadSourceFile(
                    projectRequest,
                    fileRequest);

                await Task.Delay(4000);
            }
        }

        [TestMethod]
        public async Task DownloadSourceFiles_IsSucces()
        {
            var project = new ProjectRequest { ProjectId = "28090" };
            var jobs = new JobsRequest
            {
                JobIds = [] // "30795"
            };

            var response = await _fileActions.DownloadSourceFiles(project, jobs);

            Console.WriteLine(JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
