using Apps.XTM.Actions;
using Apps.XTM.DataSourceHandlers;
using Apps.XTM.Models.Request.Projects;
using Blackbird.Applications.Sdk.Common.Dynamic;
using XTMTests.Base;

namespace Tests.XTM
{
    [TestClass]
    public class DataSources : TestBase
    {
        [TestMethod]
        public async Task DictionaryDataHandlerReturnsValues()
        {
            var action = new ProjectDataHandler(InvocationContext);

            var response = await action.GetDataAsync(new() { SearchString = "" }, CancellationToken.None);

            foreach (var project in response)
            {
                Console.WriteLine($"{project.Value} - {project.Key}");
                Assert.IsNotNull(project.Key);
            }
        }


        [TestMethod]
        public async Task GetProjectReturnsValues()
        {
            var action = new ProjectActions(InvocationContext,FileManager);
            var input = new ProjectRequest { ProjectId = "66245898" };
            var response = await action.GetProject(input);
           
            Console.WriteLine($"{response.Id} - {response.Name}");
           
        }


        [TestMethod]
        public async Task GetProjectDetailsReturnsValues()
        {
            var action = new ProjectActions(InvocationContext, FileManager);
            var input = new ProjectRequest { ProjectId = "66245898" };
            var response = await action.GetProjectDetails(input);

            Console.WriteLine($"{response.ProjectStatus.CompletionStatus}");
            Assert.IsNotNull(response.ProjectStatus.CompletionStatus);
        }
    }
}
