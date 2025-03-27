using Apps.XTM.Actions;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Response.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTMTests.Base;

namespace Tests.XTM.TestFiles
{
    [TestClass]
    public class FileTests : TestBase
    {
        [TestMethod]
        public async Task GenerateFiles_IsSucces()
        {
            var action = new FileActions(InvocationContext, FileManager);
            var project = new ProjectRequest { ProjectId = "103838155" };//107212836 
            //var project = new ProjectRequest { ProjectId = "107212836" };
            var fileGenerate = new GenerateFileRequest {FileType="XLIFF"};
            var response = await action.GenerateFiles(project, fileGenerate);
            Assert.IsNotNull(response);
        }


        [TestMethod]
        public async Task DownloadFiles_IsSucces()
        {
            var action = new FileActions(InvocationContext, FileManager);
            var project = new ProjectRequest { ProjectId = "69721875" };
            var fileGenerate = new DownloadTranslationsRequest {};
            var response = await action.DownloadTranslations(project, fileGenerate);

            int i = 0;
            foreach (var file in response.Files)
            {
                if (file.FileDescription is XtmProjectFileDescription projectFile)
                {
                    Console.WriteLine($"[{i++}] File name: {projectFile.FileName}");
                    Console.WriteLine($"    File ID: {projectFile.FileId}");
                    Console.WriteLine($"    Job ID: {projectFile.JobId}");
                    Console.WriteLine($"    Target Language: {projectFile.TargetLanguage}");
                }

            }
            Assert.IsNotNull(response);
        }
    }
}
