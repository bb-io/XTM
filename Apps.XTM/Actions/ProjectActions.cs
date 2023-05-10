using Apps.XTM.Models.Projects.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using ServiceReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Actions
{
    [ActionList]
    public class ProjectActions
    {
        [Action("Create new project", Description = "Create new project")]
        public xtmProjectResponseAPI CreateProject(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] CreateProjectRequest input)
        {
            var client = new XTMClient(authenticationCredentialsProviders);
            var projectConfiguration = new xtmProjectMTOMAPI
            {
                customer = new xtmCustomerDescriptorAPI() { id = input.CustomerId, idSpecified = true},
                name = input.ProjectName,
                sourceLanguageSpecified = true,
                sourceLanguage = (languageCODE) input.SourceLanguge,
                targetLanguages = new languageCODE?[] { (languageCODE) input.TargetLanguge },
                workflow = new xtmWorkflowDescriptorAPI
                {
                    workflowSpecified = true,
                    workflow = (xtmWORKFLOWS)input.WorkflowId
                }
            };
            var options = new xtmCreateProjectMTOMOptionsAPI
            {
                autopopulateSpecified = true,
                autopopulate = true
            };
            return client.createProjectMTOM(client.Configuration, projectConfiguration, options).project;
        }
    }
}
