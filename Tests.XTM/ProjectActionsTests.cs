using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.Actions;
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
    }
}
