using Apps.XTM.Actions;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
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
        [TestMethod]
        public async Task GenerateFiles_IsSuccess()
        {
            var action = new FileActions(InvocationContext, FileManager);
            var project = new ProjectRequest { ProjectId = "108698822" };

            var fileGenerate = new GenerateFileRequest { FileType = "XLIFF" };
            var response = await action.GenerateFiles(project, fileGenerate);

            var count = 0;

            foreach (var job in response.Files)
            {
                Console.WriteLine($"{job.FileId} - {job.FileType}");
                count++;
            }
            
            Console.WriteLine($"Total files generated: {count}");
            Assert.IsNotNull(response);


        }
    }
}
