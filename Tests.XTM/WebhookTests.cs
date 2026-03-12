using Apps.XTM.Models.Request;
using Apps.XTM.Models.Request.Customers;
using Apps.XTM.Models.Request.Projects;
using Apps.XTM.Webhooks;
using Apps.XTM.Webhooks.Models.Payload;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using Tests.XTM.Base;

namespace Tests.XTM;

[TestClass]
public class WebhookTests : TestBaseMultipleConnections
{
    [ContextDataSource, TestMethod]
    public async Task OnWorkflowTransition_ShouldNotFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var request = new WebhookRequest
        {
            Body = JsonConvert.SerializeObject(new BridgeWebhookPayload<WorkflowTransitionPayload>
            {
                Parameters = new Dictionary<string, string>
                {
                    { "xtmProjectId", "2739098" },
                    { "xtmCustomerId", "2725347" }
                },
                Payload = new WorkflowTransitionPayload
                {
                    ProjectDescriptor = new Descriptor { Id = "2739098" },
                    Events =
                    [
                        new EventPayload
                        {
                            Type = "ACTIVE",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Linguist", Id = "6271" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "translate1", WorkflowStep = "TRANSLATE1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        }
                    ]
                }
            })
        };
        var projectOptionalRequest = new ProjectOptionalRequest();
        var customerOptionalRequest = new CustomerOptionalRequest();
        var workflowOptionalRequest = new WorkflowStepOptionalRequest() { WorkflowStep = "Automated post-editing1" };

        // Act
        var result = await webhookList.OnWorkflowTransition(
            request,
            projectOptionalRequest,
            customerOptionalRequest,
            workflowOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
    }

    [ContextDataSource, TestMethod]
    public async Task OnWorkflowTransition_ShouldFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var request = new WebhookRequest
        {
            Body = JsonConvert.SerializeObject(new BridgeWebhookPayload<WorkflowTransitionPayload>
            {
                Parameters = new Dictionary<string, string>
                {
                    { "xtmProjectId", "2739098" },
                    { "xtmCustomerId", "2725347" }
                },
                Payload = new WorkflowTransitionPayload
                {
                    ProjectDescriptor = new Descriptor { Id = "2739098" },
                    Events =
                    [
                        new EventPayload
                        {
                            Type = "FINISHED",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Linguist", Id = "6271" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "translate1", WorkflowStep = "TRANSLATE1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        },
                        new EventPayload
                        {
                            Type = "ACTIVE",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Blackbird", Id = "92530" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "Automated post-editing1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        }
                    ]
                }
            })
        };
        var projectOptionalRequest = new ProjectOptionalRequest();
        var customerOptionalRequest = new CustomerOptionalRequest();
        var workflowOptionalRequest = new WorkflowStepOptionalRequest() { WorkflowStep = "Automated post-editing1" };

        // Act
        var result = await webhookList.OnWorkflowTransition(
            request,
            projectOptionalRequest,
            customerOptionalRequest,
            workflowOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Default, result.ReceivedWebhookRequestType);
    }

    [ContextDataSource, TestMethod]
    public async Task OnWorkflowTransition_WithProjectNameFilter_ShouldFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var request = new WebhookRequest
        {
            Body = JsonConvert.SerializeObject(new BridgeWebhookPayload<WorkflowTransitionPayload>
            {
                Parameters = new Dictionary<string, string>
                {
                    { "xtmProjectId", "2739098" },
                    { "xtmCustomerId", "2725347" }
                },
                Payload = new WorkflowTransitionPayload
                {
                    ProjectDescriptor = new Descriptor { Id = "2739098" },
                    Events =
                    [
                        new EventPayload
                        {
                            Type = "FINISHED",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Linguist", Id = "6271" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "translate1", WorkflowStep = "TRANSLATE1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        },
                        new EventPayload
                        {
                            Type = "ACTIVE",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Blackbird", Id = "92530" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "Automated post-editing1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        }
                    ]
                }
            })
        };
        var projectOptionalRequest = new ProjectOptionalRequest() { ProjectNameContains = "TAUS automated post-editing" };
        var customerOptionalRequest = new CustomerOptionalRequest();
        var workflowOptionalRequest = new WorkflowStepOptionalRequest() { WorkflowStep = "Automated post-editing1" };

        // Act
        var result = await webhookList.OnWorkflowTransition(
            request,
            projectOptionalRequest,
            customerOptionalRequest,
            workflowOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Default, result.ReceivedWebhookRequestType);
    }

    [ContextDataSource, TestMethod]
    public async Task OnWorkflowTransition_WhenEventIsFinished_ShouldNotFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var request = new WebhookRequest
        {
            Body = JsonConvert.SerializeObject(new BridgeWebhookPayload<WorkflowTransitionPayload>
            {
                Parameters = new Dictionary<string, string>
                {
                    { "xtmProjectId", "2739098" },
                    { "xtmCustomerId", "2725347" }
                },
                Payload = new WorkflowTransitionPayload
                {
                    ProjectDescriptor = new Descriptor { Id = "2739098" },
                    Events =
                    [
                        new EventPayload
                        {
                            Type = "FINISHED",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Blackbird", Id = "92530" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "Automated post-editing1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        },
                        new EventPayload
                        {
                            Type = "ACTIVE",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Linguist", Id = "6271" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "translate1", WorkflowStep = "TRANSLATE1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        }
                    ]
                }
            })
        };
        var projectOptionalRequest = new ProjectOptionalRequest();
        var customerOptionalRequest = new CustomerOptionalRequest();
        var workflowOptionalRequest = new WorkflowStepOptionalRequest() { WorkflowStep = "Automated post-editing1" };

        // Act
        var result = await webhookList.OnWorkflowTransition(
            request,
            projectOptionalRequest,
            customerOptionalRequest,
            workflowOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
    }

    [ContextDataSource, TestMethod]
    public async Task OnWorkflowTransitionManual_ShouldNotFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var request = new WebhookRequest
        {
            Body = JsonConvert.SerializeObject(new BridgeWebhookPayload<WorkflowTransitionPayload>
            {
                Parameters = new Dictionary<string, string>
                {
                    { "xtmProjectId", "2739098" },
                    { "xtmCustomerId", "2725347" }
                },
                Payload = new WorkflowTransitionPayload
                {
                    ProjectDescriptor = new Descriptor { Id = "2739098" },
                    Events =
                    [
                        new EventPayload
                        {
                            Type = "ACTIVE",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Linguist", Id = "6271" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "translate1", WorkflowStep = "TRANSLATE1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        }
                    ]
                }
            })
        };
        var projectOptionalRequest = new ProjectOptionalRequest();
        var customerOptionalRequest = new CustomerOptionalRequest();
        var workflowOptionalRequest = new WorkflowStepOptionalRequest() { WorkflowStep = "Automated post-editing1" };

        // Act
        var result = await webhookList.OnWorkflowTransitionManual(
            request,
            projectOptionalRequest,
            customerOptionalRequest,
            workflowOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
    }

    [ContextDataSource, TestMethod]
    public async Task OnWorkflowTransitionManual_ShouldFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var request = new WebhookRequest
        {
            Body = JsonConvert.SerializeObject(new BridgeWebhookPayload<WorkflowTransitionPayload>
            {
                Parameters = new Dictionary<string, string>
                {
                    { "xtmProjectId", "2739098" },
                    { "xtmCustomerId", "2725347" }
                },
                Payload = new WorkflowTransitionPayload
                {
                    ProjectDescriptor = new Descriptor { Id = "2739098" },
                    Events =
                    [
                        new EventPayload
                        {
                            Type = "FINISHED",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Linguist", Id = "6271" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "translate1", WorkflowStep = "TRANSLATE1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        },
                        new EventPayload
                        {
                            Type = "ACTIVE",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Blackbird", Id = "92530" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "Automated post-editing1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        }
                    ]
                }
            })
        };
        var projectOptionalRequest = new ProjectOptionalRequest();
        var customerOptionalRequest = new CustomerOptionalRequest();
        var workflowOptionalRequest = new WorkflowStepOptionalRequest() { WorkflowStep = "Automated post-editing1" };

        // Act
        var result = await webhookList.OnWorkflowTransitionManual(
            request,
            projectOptionalRequest,
            customerOptionalRequest,
            workflowOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Default, result.ReceivedWebhookRequestType);
    }

    [ContextDataSource, TestMethod]
    public async Task OnWorkflowTransitionManual_WithProjectNameFilter_ShouldFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var request = new WebhookRequest
        {
            Body = JsonConvert.SerializeObject(new BridgeWebhookPayload<WorkflowTransitionPayload>
            {
                Parameters = new Dictionary<string, string>
                {
                    { "xtmProjectId", "2739098" },
                    { "xtmCustomerId", "2725347" }
                },
                Payload = new WorkflowTransitionPayload
                {
                    ProjectDescriptor = new Descriptor { Id = "2739098" },
                    Events =
                    [
                        new EventPayload
                        {
                            Type = "FINISHED",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Linguist", Id = "6271" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "translate1", WorkflowStep = "TRANSLATE1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        },
                        new EventPayload
                        {
                            Type = "ACTIVE",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Blackbird", Id = "92530" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "Automated post-editing1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        }
                    ]
                }
            })
        };
        var projectOptionalRequest = new ProjectOptionalRequest() { ProjectNameContains = "TAUS automated post-editing" };
        var customerOptionalRequest = new CustomerOptionalRequest();
        var workflowOptionalRequest = new WorkflowStepOptionalRequest() { WorkflowStep = "Automated post-editing1" };

        // Act
        var result = await webhookList.OnWorkflowTransitionManual(
            request,
            projectOptionalRequest,
            customerOptionalRequest,
            workflowOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Default, result.ReceivedWebhookRequestType);
    }

    [ContextDataSource, TestMethod]
    public async Task OnWorkflowTransitionManual_WhenEventIsFinished_ShouldNotFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var request = new WebhookRequest
        {
            Body = JsonConvert.SerializeObject(new BridgeWebhookPayload<WorkflowTransitionPayload>
            {
                Parameters = new Dictionary<string, string>
                {
                    { "xtmProjectId", "2739098" },
                    { "xtmCustomerId", "2725347" }
                },
                Payload = new WorkflowTransitionPayload
                {
                    ProjectDescriptor = new Descriptor { Id = "2739098" },
                    Events =
                    [
                        new EventPayload
                        {
                            Type = "FINISHED",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Blackbird", Id = "92530" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "Automated post-editing1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        },
                        new EventPayload
                        {
                            Type = "ACTIVE",
                            Tasks =
                            [
                                new TaskPayload
                                {
                                    CurrentUser = new UserPayload { Name = "Linguist", Id = "6271" },
                                    Filename = "Sample text.html",
                                    TargetLanguage = "de_001",
                                    Step = new WorkflowStepPayload { WorkflowStepName = "translate1", WorkflowStep = "TRANSLATE1" },
                                    Job = new Descriptor { Id = "2739109" }
                                }
                            ]
                        }
                    ]
                }
            })
        };
        var projectOptionalRequest = new ProjectOptionalRequest();
        var customerOptionalRequest = new CustomerOptionalRequest();
        var workflowOptionalRequest = new WorkflowStepOptionalRequest() { WorkflowStep = "Automated post-editing1" };

        // Act
        var result = await webhookList.OnWorkflowTransitionManual(
            request,
            projectOptionalRequest,
            customerOptionalRequest,
            workflowOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
    }

    [ContextDataSource, TestMethod]
    public async Task OnWorkflowTransitionManual_WhenReceivingFormEncoding_ShouldFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var jsonPayload = JsonConvert.SerializeObject(new WorkflowTransitionPayload
        {
            ProjectDescriptor = new Descriptor { Id = "2739098" },
            Events =
                [
                    new EventPayload
                    {
                        Type = "FINISHED",
                        Tasks =
                        [
                            new TaskPayload
                            {
                                CurrentUser = new UserPayload { Name = "Linguist", Id = "6271" },
                                Filename = "Sample text.html",
                                TargetLanguage = "de_001",
                                Step = new WorkflowStepPayload { WorkflowStepName = "translate1", WorkflowStep = "TRANSLATE1" },
                                Job = new Descriptor { Id = "2739109" }
                            }
                        ]
                    },
                    new EventPayload
                    {
                        Type = "ACTIVE",
                        Tasks =
                        [
                            new TaskPayload
                            {
                                CurrentUser = new UserPayload { Name = "Blackbird", Id = "92530" },
                                Filename = "Sample text.html",
                                TargetLanguage = "de_001",
                                Step = new WorkflowStepPayload { WorkflowStepName = "Automated post-editing1" },
                                Job = new Descriptor { Id = "2739109" }
                            }
                        ]
                    }
                ]
        });
        var request = new WebhookRequest
        {
            QueryParameters = new Dictionary<string, string>
            {
                { "xtmProjectId", "2739098" },
                { "xtmCustomerId", "2725347" }
            },
            Body = $"additionalData={Uri.EscapeDataString(jsonPayload)}"
        };
        var projectOptionalRequest = new ProjectOptionalRequest() { ProjectNameContains = "TAUS automated post-editing" };
        var customerOptionalRequest = new CustomerOptionalRequest();
        var workflowOptionalRequest = new WorkflowStepOptionalRequest() { WorkflowStep = "Automated post-editing1" };

        // Act
        var result = await webhookList.OnWorkflowTransitionManual(
            request,
            projectOptionalRequest,
            customerOptionalRequest,
            workflowOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Default, result.ReceivedWebhookRequestType);
    }

    [ContextDataSource, TestMethod]
    public async Task OnProjectFinished_ShouldFlight(InvocationContext context)
    {
        // Arrange
        var webhookList = new WebhookList(context);
        var jsonPayload = "{\"Parameters\":{\"id\":\"73b9fa64184e3e73b2b27964bfcab4b62d1c13e9278ca353fd9968eded875989\",\"eventType\":\"projectFinished\",\"xtmProjectId\":\"206025963\",\"xtmCustomerId\":\"19\",\"xtmUuid\":\"9befef0f-2ef1-4390-abcf-9c291b960b4d\"},\"Payload\":null}";
        var request = new WebhookRequest
        {
            QueryParameters = new Dictionary<string, string>(),
            Body = jsonPayload
        };
        var projectOptionalRequest = new ProjectOptionalRequest() { ProjectId = "206025963" };

        // Act
        var result = await webhookList.OnProjectFinished(
            request,
            projectOptionalRequest);

        // Assert
        PrintResult(result);
        Assert.AreEqual(WebhookRequestType.Default, result.ReceivedWebhookRequestType);
    }
}
