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

            var response = await action.ListProjects(new Apps.XTM.Models.Request.Projects.ListProjectsRequest() {});

            Assert.IsNotNull(response.Projects);
        }

        [TestMethod]
        public async Task DownloadProjectFile_successful()
        {
            var action = new FileActions(InvocationContext, FileManager);
            var projectrequest= new ProjectRequest { ProjectId = "105836057" };
            var fileRequest = new DownloadProjectFileRequest { FileId = "106028856", FileScope= "JOB" };
            var response = await action.DownloadProjectFile(projectrequest, fileRequest);

            Assert.IsNotNull(response);
        }
    }
}
