using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Models.Request.TranslationMemory;
using Blackbird.Applications.Sdk.Common.Files;
using XTMTests.Base;

namespace Tests.XTM
{
    [TestClass]
    public class ProjectActionsTests : TestBase
    {
        [TestMethod]
        public async Task ListProject_successful()
        {
            var action = new ProjectActions(InvocationContext,FileManager);

            var response = await action.ListProjects(new ListProjectsRequest() { CustomerIds = ["35961951"] });

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(json);

            Assert.IsNotNull(response.Projects);
        }

        [TestMethod]
        public async Task DownloadProjectFile_successful()
        {
            var action = new FileActions(InvocationContext, FileManager);
            var projectrequest = new ProjectRequest { ProjectId = "187237893" };
            var fileRequest = new DownloadProjectFileRequest { FileId = "187430508", FileScope = "PROJECT" };

            //var projectrequest = new ProjectRequest { ProjectId = "187028359" };
            //var fileRequest = new DownloadProjectFileRequest { FileId = "187451330", FileScope = "PROJECT" };
            var response = await action.DownloadProjectFile(projectrequest, fileRequest);

            Assert.IsNotNull(response);
        }

        //
        [TestMethod]
        public async Task DownloadProjectFiles_successful()
        {
            var action = new FileActions(InvocationContext, FileManager);
            //var projectrequest = new ProjectRequest { ProjectId = "187189509" };
            //var fileRequest = new DownloadAllProjectFilesRequest { FileScope = "PROJECT", FileType= "HTML_EXTENDED_TABLE" };

            var projectrequest = new ProjectRequest { ProjectId = "187363559" };
            var fileRequest = new DownloadAllProjectFilesRequest { FileScope = "PROJECT", FileType = "HTML_EXTENDED_TABLE" };

            var response = await action.DownloadProjectFiles(projectrequest, fileRequest);

            Assert.IsNotNull(response);
        }

        [TestMethod]
        public async Task GetProjectStatus_successful()
        {
            var action = new ProjectActions(InvocationContext, FileManager);

            var response = await action.GetProjectStatus(new ProjectRequest { ProjectId= "113735837" });

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(json);

            Assert.IsNotNull(response);
        }

        [TestMethod]
        public async Task GetProjectEstimates_successful()
        {
            var action = new ProjectActions(InvocationContext, FileManager);

            var response = await action.GetProjectEstimates(new ProjectRequest { ProjectId = "74644428" });

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(json);

            Assert.IsNotNull(response);
        }
    }
}