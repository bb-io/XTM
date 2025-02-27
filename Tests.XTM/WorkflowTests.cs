using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.Actions;
using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Files;
using Apps.XTM.Models.Request.Projects;
using XTMTests.Base;

namespace Tests.XTM
{
    [TestClass]
    public class WorkflowTests:TestBase
    {
        [TestMethod]
        public async Task MoveWorkflowsToNextStep_ReturnsValue()
        {
            var action = new WorkflowActions(InvocationContext);
            var project = new ProjectRequest { ProjectId=""};
            var jobs = new JobsRequest { JobIds = new List<string> { "" } };
            var mailing = new MailingRequest { Mailing = "DISABLED" };

            var response = await action.MoveJobsToNextWorkflowStep(jobs, project, mailing);
        }
    }
}
