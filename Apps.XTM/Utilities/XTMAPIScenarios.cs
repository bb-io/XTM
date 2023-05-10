using System;
using System.IO;
using System.Threading;
using System.Net;

namespace Apps.XTM.Utilities
{
    using ServiceReference;
    using System.Collections.Generic;
    using System.Linq;

    class XTMAPIScenarios : XTMAPIHelper
    {
        public XTMAPIScenarios(XTMAPIConfiguration configuration) : base(configuration)
        {
        }

        public void GetXTMInfo()
        {
            try
            {
                xtmGetXTMInfoResponseAPI response = GetWebService().getXTMInfo(Configuration.LoginAPI, null);

                Console.WriteLine("Company name: " + response.xtmInfo.companyName);
                Console.WriteLine("Logo: " + response.xtmInfo.logo);
                Console.WriteLine("Website: " + response.xtmInfo.website);
                Console.WriteLine("Version: " + response.xtmInfo.version + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void SupportedAndNotSupportedFilesInfo()
        {
            try
            {
                xtmGetSupportedFilesInfoResponseAPI response = GetWebService().getSupportedFilesInfo(Configuration.LoginAPI, null);

                Console.WriteLine("Supported files:");

                foreach (xtmSupportedFileExtResponseAPI supportedFile in response.supportedFilesInfo.supportedFiles)
                {
                    Console.Write(supportedFile.extension + ", ");
                }

                Console.WriteLine("\n");
                Console.WriteLine("Not supported files:");

                foreach (xtmNotSupportedFileExtResponseAPI notSupportedFile in response.supportedFilesInfo.notSupportedFiles)
                {
                    Console.Write(notSupportedFile.extension + ", ");
                }
                Console.WriteLine("\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void CreateTemplate()
        {
            try
            {
                Console.WriteLine("Create template.");

                Random random = new Random();

                xtmTemplateAPI template = new xtmTemplateAPI
                {
                    name = "Example template " + random.Next(10000),
                    description = "Example description"
                };

                xtmExternalTemplateDescriptorAPI externalTemplateDescriptor = new xtmExternalTemplateDescriptorAPI
                {
                    externalId = "my-id-" + random.Next(100, 1000000)
                };

                template.externalDescriptor = externalTemplateDescriptor;

                xtmCreateTemplateResponseAPI response = GetWebService().createTemplate(Configuration.LoginAPI, template, null);

                if (response != null)
                {
                    Console.WriteLine("Template ID: " + response.template.templateDescriptor.id + ", name: " + response.template.name + "\n");
                    ObtainPMTemplateEditorLink(response.template.templateDescriptor);
                }
                else
                {
                    Console.WriteLine("Template not created.\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }

        }

        public void ObtainLinks(xtmProjectDescriptorAPI project, List<xtmJobDescriptorAPI> jobsDescriptors)
        {
            ObtainPMProjectEditorLink(project);
        }


        public void ObtainPMTemplateEditorLink(xtmTemplateDescriptorAPI template)
        {
            try
            {
                var options = new xtmObtainPMTemplateEditorLinkOptionsAPI
                {
                    templateEditorOptions = new pmTemplateEditorOptions
                    {
                        showMachineTranslationSettings = true,
                        showMachineTranslationSettingsSpecified = true,
                        showTranslationSettings = true,
                        showTranslationSettingsSpecified = true
                    }
                };

                xtmObtainPMTemplateEditorLinkResponseAPI response = GetWebService().obtainPMTemplateEditorLink(Configuration.LoginAPI, template, options);

                Console.WriteLine("Template editor link: " + response.templateEditor.templateEditorLink + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public xtmProjectResponseAPI CreateProjectMTOM()
        {
            try
            {
                Console.WriteLine("Create project.");
                Random random = new Random();


                var translationFiles = Configuration.Translations.Select(fileUri => new xtmFileMTOMAPI
                {
                    fileName = fileUri.AbsoluteUri,
                    fileMTOM = new WebClient().DownloadData(fileUri.AbsoluteUri)
                }).ToList();

                var project = new xtmProjectMTOMAPI
                {
                    customer = Configuration.Customer,
                    name = "API project_" + random.Next(1, 100),
                    externalDescriptor = new xtmExternalProjectDescriptorAPI
                    {
                        integrationId = random.Next(1, 1000) + ""
                    },
                    sourceLanguageSpecified = true,
                    sourceLanguage = Configuration.SourceLanguage,
                    targetLanguages = Configuration.TargetLanguages,
                    workflow = new xtmWorkflowDescriptorAPI
                    {
                        workflowSpecified = true,
                        workflow = xtmWORKFLOWS.TRANSLATE_P_CORRECT_F_REVIEW
                    },
                    projectManagerTypeSpecified = true,
                    projectManagerType = xtmProjectManagerTypeEnum.FROM_CUSTOMER,
                    translationFiles = translationFiles.ToArray(),
                    useTerminologyDecorationSpecified = true,
                    useTerminologyDecoration = true,
                    allowNotApproveTermSpecified = true,
                    allowNotApproveTerm = xtmTermAllowNotApproveEnum.ALLOW
                };

                var options = new xtmCreateProjectMTOMOptionsAPI
                {
                    autopopulateSpecified = true,
                    autopopulate = true
                };

                xtmCreateProjectMTOMResponseAPI response = GetWebService().createProjectMTOM(Configuration.LoginAPI, project, options);
                return response.project;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
                return null;
            }
        }

        public xtmProjectResponseAPI CreateProjectURL()
        {
            try
            {
                Console.WriteLine("Create project.");
                Random random = new Random();

                var project = new xtmProjectURLAPI
                {
                    customer = Configuration.Customer,
                    name = "API project_" + random.Next(1, 100),
                    externalDescriptor = new xtmExternalProjectDescriptorAPI
                    {
                        integrationId = random.Next(1, 1000) + ""
                    },
                    sourceLanguageSpecified = true,
                    sourceLanguage = Configuration.SourceLanguage,
                    targetLanguages = Configuration.TargetLanguages,
                    workflow = new xtmWorkflowDescriptorAPI
                    {
                        workflowSpecified = true,
                        workflow = xtmWORKFLOWS.TRANSLATE_P_CORRECT_F_REVIEW
                    },
                    projectManagerTypeSpecified = true,
                    projectManagerType = xtmProjectManagerTypeEnum.FROM_CUSTOMER,
                    translationFiles = Configuration.Translations.Select(fileUri => new xtmFileURLAPI()
                    {
                        fileName = fileUri.AbsoluteUri,
                        fileURL = fileUri.AbsoluteUri
                    }).ToArray(),
                    useTerminologyDecorationSpecified = true,
                    useTerminologyDecoration = true,
                    allowNotApproveTermSpecified = true,
                    allowNotApproveTerm = xtmTermAllowNotApproveEnum.ALLOW
                };

                var options = new xtmCreateProjectURLOptionsAPI
                {
                    autopopulateSpecified = true,
                    autopopulate = true
                };

                xtmCreateProjectURLResponseAPI response = GetWebService().createProjectURL(Configuration.LoginAPI, project, options);
                return response.project;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
                return null;
            }
        }

        public void CheckProjectAnalysisCompletion(xtmProjectDescriptorAPI project)
        {
            try
            {
                while (true)
                {
                    xtmCheckProjectAnalysisCompletionResponseAPI response = GetWebService().checkProjectAnalysisCompletion(Configuration.LoginAPI, project, null);

                    if (xtmPROJECTCOMPLETIONSTATUS.FINISHED.Equals(response.project.status))
                    {
                        Console.WriteLine("Analysis is finished." + "\n");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Analysis isn't finished." + "\n");
                    }

                    Thread.Sleep(2000);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void ObtainProjectMetrics(xtmProjectDescriptorAPI project)
        {
            try
            {
                Console.WriteLine("Project metrics.");

                xtmObtainProjectMetricsOptionsAPI options = new xtmObtainProjectMetricsOptionsAPI
                {
                    targetLanguageSpecified = true,
                    targetLanguage = (languageCODE)Configuration.TargetLanguages[0]
                };

                xtmProjectMetricsResponseAPI[] response = GetWebService().obtainProjectMetrics(Configuration.LoginAPI, project, options);

                if (response != null)
                {
                    foreach (xtmProjectMetricsResponseAPI projectMetrics in response)
                    {
                        Console.WriteLine("Project ID: " + projectMetrics.projectDescriptor.id);
                        Console.WriteLine("Target language: " + projectMetrics.targetLanguage);
                        Console.WriteLine("Total words: " + projectMetrics.coreMetrics.totalWords);

                        xtmProjectMetricsResponseAPIEntry[] metricsProgress = projectMetrics.metricsProgressMap;

                        if (metricsProgress != null)
                        {
                            foreach (xtmProjectMetricsResponseAPIEntry entry in metricsProgress)
                            {
                                Console.WriteLine("Progress for " + entry.key + ": words done " + entry.value.wordsDone);
                            }
                        }
                        Console.WriteLine("\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void ObtainPMProjectEditorLink(xtmProjectDescriptorAPI project)
        {
            try
            {
                var editorOptions = new pmProjectEditorOptions
                {
                    defaultTabSpecified = true,
                    defaultTab = pmProjectEditorTabEnum.WORKFLOW,
                    showEstimatesSpecified = true,
                    showEstimates = false
                };

                var options = new xtmObtainPMProjectEditorLinkOptionsAPI
                {
                    projectEditorOptions = editorOptions
                };

                xtmObtainPMProjectEditorLinkResponseAPI response = GetWebService().obtainPMProjectEditorLink(Configuration.LoginAPI, project, options);

                Console.WriteLine("Project editor link: " + response.projectEditor.projectEditorLink + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void CheckProjectCompletion(xtmProjectDescriptorAPI project)
        {
            try
            {
                Console.WriteLine("Check project completion.");

                xtmCheckProjectCompletionResponseAPI response = GetWebService().checkProjectCompletion(Configuration.LoginAPI, project, null);

                if (response != null)
                {
                    Console.WriteLine("Project: " + response.project.projectDescriptor.id);
                    Console.WriteLine("Project activity: " + response.project.activity);
                    Console.WriteLine("Project status: " + response.project.status);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void ObtainJobMetrics(List<xtmJobDescriptorAPI> jobsDescriptors)
        {
            try
            {
                Console.WriteLine("Job metrics.");

                List<xtmJobMetricsResponseAPI> response = GetWebService().obtainJobMetrics(Configuration.LoginAPI, jobsDescriptors.ToArray(), null).ToList();

                if (response != null)
                {
                    foreach (xtmJobMetricsResponseAPI jobMetrics in response)
                    {
                        Console.WriteLine("Job ID: " + jobMetrics.jobDescriptor.id);
                        Console.WriteLine("Total characters: " + jobMetrics.coreMetrics.totalCharacters);

                        List<xtmJobMetricsResponseAPIEntry> progressMap = jobMetrics.metricsProgressMap.ToList();

                        if (progressMap != null)
                        {
                            foreach (xtmJobMetricsResponseAPIEntry entry in progressMap)
                            {
                                Console.WriteLine("Progress for " + entry.key + ": words done " + entry.value.wordsDone);
                            }
                        }

                        Console.WriteLine("\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void AssignLinguistToJob(List<xtmJobDescriptorAPI> jobsDescriptors)
        {
            try
            {
                Console.WriteLine("Assign linguist.");
                xtmWorkflowStepDescriptorAPI workflowStepDescriptior = new xtmWorkflowStepDescriptorAPI
                {
                    workflowStepSpecified = true,
                    workflowStep = xtmWORKFLOWSTEP.TRANSLATE1
                };

                xtmStepLinguistAssignmentAPI stepLinguistAssigment = new xtmStepLinguistAssignmentAPI
                {
                    user = Configuration.Linguist,
                    step = workflowStepDescriptior
                };

                List<xtmJobLinguistAssignmentAPI> jobLinguistAssigments = jobsDescriptors.Select(jobDescriptor => new xtmJobLinguistAssignmentAPI
                {
                    stepLinguistAssignments = new xtmStepLinguistAssignmentAPI[] { stepLinguistAssigment },
                    jobDescriptor = jobDescriptor
                }).ToList();

                xtmJobLinguistAssignmentResponseAPI[] response = GetWebService().assignLinguistToJob(Configuration.LoginAPI, jobLinguistAssigments.ToArray(), null);

                if (response != null)
                {
                    foreach (xtmJobLinguistAssignmentResponseAPI jobLinguistAssigment in response)
                    {
                        Console.WriteLine("Job ID: " + jobLinguistAssigment.jobDescriptor.id + ", result: " + jobLinguistAssigment.result);
                    }
                }
                Console.WriteLine("\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void StartProject(xtmProjectDescriptorAPI project)
        {
            try
            {
                Console.WriteLine("Start project.");

                xtmProjectStartResponseAPI[] response = GetWebService().startProject(Configuration.LoginAPI, new xtmProjectDescriptorAPI[] { project }, null);

                if (response != null)
                {
                    foreach (xtmProjectStartResponseAPI projectStartResponse in response)
                    {
                        Console.WriteLine("Project ID: " + projectStartResponse.projectDescriptor.id + ", result: " + projectStartResponse.result + "\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void UpdateJobStepProperty(xtmJobDescriptorAPI job)
        {
            try
            {
                var stepsToUpdate = new xtmStepPropertyAPI[]
                           {
                               new xtmStepPropertyAPI()
                               {
                                   dueDate=DateTime.Now.AddDays(1),
                                   dueDateSpecified=true,
                                   stepDescriptor=new xtmWorkflowStepDescriptorAPI()
                                   {
                                       workflowStep=xtmWORKFLOWSTEP.TRANSLATE1,
                                       workflowStepSpecified=true,
                                   }
                               }
                           };

                var updates = new xtmUpdateJobStepPropertyAPI()
                {
                    jobs = new xtmJobStepPropertyAPI[]
                   {
                       new xtmJobStepPropertyAPI()
                       {
                           jobDescriptor=job,
                           steps=stepsToUpdate
                       }
                   }
                };

                var response = GetWebService().updateJobStepProperty(Configuration.LoginAPI, updates, new xtmUpdateJobStepPropertyOptionsAPI());
                if (response.result == true)
                {
                    Console.WriteLine($"Successfully updated job {job.id} workflow step (TRANSLATE due date - tomorrow).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void MoveJobWorkflow(xtmJobDescriptorAPI job)
        {
            try
            {
                var workflowJobMove = new xtmJobWorkflowMoveAPI()
                {
                    jobDescriptor = job,
                    move = xtmWORKFLOWMOVE.FORWARD,
                    moveSpecified = true
                };

                var response = GetWebService().moveJobWorkflow(Configuration.LoginAPI, new xtmJobWorkflowMoveAPI[] { workflowJobMove }, new xtmMoveJobWorkflowOptionsAPI());
                if (response.jobsResult.First().result == true)
                {
                    Console.WriteLine($"Successfully moved job {job.id} workflow: step forward.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void MoveProjectWorkflow(xtmProjectDescriptorAPI project)
        {
            try
            {
                var workflowProjectMove = new xtmMoveProjectWorkflowAPI()
                {
                    projectDescriptor = project,
                    move = xtmWORKFLOWMOVE.RESTART,
                    moveSpecified = true
                };

                var response = GetWebService().moveProjectWorkflow(Configuration.LoginAPI, workflowProjectMove, new xtmMoveProjectWorkflowOptionsAPI());
                if (response.jobsResult.First().result == true)
                {
                    Console.WriteLine($"Successfully moved project {project.id} workflow: step forward.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }


        public List<xtmJobFileBaseResponseAPI> GenerateJobFile(List<xtmJobDescriptorAPI> jobsDescriptors)
        {
            try
            {
                Console.WriteLine("Generate job File.");

                List<xtmJobFileBaseResponseAPI> result = null;

                xtmGenerateJobFileOptionsAPI options = new xtmGenerateJobFileOptionsAPI
                {
                    fileTypeSpecified = true,
                    fileType = generatedFILETYPE.TARGET
                };

                List<xtmJobFileBaseResponseAPI> response = GetWebService().generateJobFile(Configuration.LoginAPI, jobsDescriptors.ToArray(), options).ToList();

                if (response != null)
                {
                    foreach (xtmJobFileBaseResponseAPI jobFileBaseResponse in response)
                    {
                        Console.WriteLine("Job ID: " + jobFileBaseResponse.jobDescriptor.id + ", file ID: " + jobFileBaseResponse.fileDescriptor.id + ", file type: "
                                + jobFileBaseResponse.fileType);
                    }

                    result = response;
                }

                Console.WriteLine("\n");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
                return null;
            }
        }

        public void CheckJobFileCompletion(List<xtmFileDescriptorAPI> fileDescriptors)
        {
            while (true)
            {
                try
                {
                    xtmJobFileStatusResponseAPI[] response = GetWebService().checkJobFileCompletion(Configuration.LoginAPI, fileDescriptors.ToArray(), null);

                    if (response.Length > 0)
                    {
                        xtmJOBFILECOMPLETIONSTATUS jobStatus = response[0].status;

                        if (xtmJOBFILECOMPLETIONSTATUS.FINISHED.Equals(jobStatus))
                        {
                            Console.WriteLine("File generation finished successfully.");
                            break;
                        }
                        else if (xtmJOBFILECOMPLETIONSTATUS.ERROR.Equals(jobStatus))
                        {
                            Console.WriteLine("File generation failed.");
                            break;
                        }
                        else
                        {
                            Thread.Sleep(5000);
                        }

                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.InnerException);
                }
            }
        }

        public void DownloadJobFileURL(List<xtmFileDescriptorAPI> fileDescriptors)
        {
            try
            {
                Console.WriteLine("Download job file.");

                List<xtmJobFileURLResponseAPI> response = GetWebService().downloadJobFileURL(Configuration.LoginAPI, fileDescriptors.ToArray(), null).ToList();

                if (response != null)
                {
                    foreach (xtmJobFileURLResponseAPI jobFileURLResponse in response)
                    {
                        Console.WriteLine("Job ID: " + jobFileURLResponse.jobDescriptor.id + ", original file: " + jobFileURLResponse.originalFileName + ", file URL: "
                                + jobFileURLResponse.fileURL);

                        WebClient webClient = new WebClient();

                        if (!Directory.Exists(Configuration.DownloadDirectory))
                        {
                            Directory.CreateDirectory(Configuration.DownloadDirectory);
                        }

                        webClient.DownloadFile(jobFileURLResponse.fileURL, Configuration.DownloadDirectory + jobFileURLResponse.targetLanguage + "_" + jobFileURLResponse.fileName);

                        Console.WriteLine("Downloaded as: " + Configuration.DownloadDirectory + jobFileURLResponse.targetLanguage + "_" + jobFileURLResponse.fileName + "\n");

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }

        public void FindUpdateAndDeleteProject(xtmProjectDescriptorAPI projectDescriptor)
        {
            try
            {
                Console.WriteLine("Find - update - delete project.");
                var projectFilter = new xtmFilterProjectAPI() // get newly created project
                {
                    customers = new xtmCustomerDescriptorAPI[] { Configuration.Customer },
                    projects = new xtmProjectDescriptorAPI[] { projectDescriptor }
                };
                var searchResult = GetWebService().findProject(Configuration.LoginAPI, projectFilter,
                    new xtmFindProjectOptionsAPI() { projectCreator = xtmProjectCreatorEnum.LOGIN_API_USER, projectCreatorSpecified = true });

                var projectFound = searchResult.projects.Single();
                Console.WriteLine($"Found {searchResult.projects.Count()} project (name: {projectFound.name})");


                var updateProjectApi = new xtmUpdateProjectAPI()
                {
                    projectDescriptor = projectFound.projectDescriptor,
                    name = projectFound.name + "- updated"
                };

                var updateResult = GetWebService().updateProject(Configuration.LoginAPI, new xtmUpdateProjectAPI[] { updateProjectApi }, new xtmUpdateProjectOptionsAPI());
                if (updateResult.Single().result == true)
                    Console.WriteLine($"Updated {updateResult.Count()} project (new name: {updateProjectApi.name})");

                var deleteResult = GetWebService().updateProjectActivity(Configuration.LoginAPI, new xtmProjectDescriptorAPI[] { projectDescriptor }
                , new xtmUpdateProjectActivityOptionsAPI() { activitySpecified = true, activity = xtmPROJECTACTIVITY.DELETE_WITH_TM });
                if (deleteResult.Single().result == true)
                    Console.WriteLine($"Deleted project {deleteResult.Single().projectDescriptor.id} with corresponding TM.");

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public void ObtainMetricsStatisticsAndCosts(xtmProjectDescriptorAPI project, List<xtmJobDescriptorAPI> jobsDescriptors)
        {
            ObtainJobMetrics(jobsDescriptors);
            ObtainProjectMetrics(project);
            DownloadProjectMetricsURL(project);
            //DownloadProjectMetricsMTOM(project);
            ObtainProjectStatistics(project);
            //ObtainProjectAllStatistics(project);         
            ObtainProjectEstimates(project);
            ObtainProjectsSimilarity(GenerateProjectsSimilarity(project));
        }
        public void ObtainProjectsSimilarity(xtmSimilarityDescriptorAPI similarityDescriptorAPI)
        {
            try
            {

                var result = GetWebService().obtainProjectsSimilarity(Configuration.LoginAPI, new xtmSimilarityDescriptorAPI[] { similarityDescriptorAPI },
                    new xtmObtainProjectsSimilarityOptionsAPI()).First();

                Console.WriteLine("Project similarities obtained.");
                var metrics = result.metrics;
                Console.WriteLine($"- Ice matches: {metrics.iceMatches}");
                Console.WriteLine($"- Leveraged: {metrics.leveraged}");
                Console.WriteLine($"- Repetitions: {metrics.repetitions}");
                Console.WriteLine($"- Total: {metrics.total}");


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public xtmSimilarityDescriptorAPI GenerateProjectsSimilarity(xtmProjectDescriptorAPI project)
        {
            try
            {
                var projectPair = new xtmProjectsSimilarityPairAPI()
                {
                    baseProject = project,
                    projectToCompare = project
                };

                return GetWebService().generateProjectsSimilarity(Configuration.LoginAPI, new xtmProjectsSimilarityPairAPI[] { projectPair }, null)
                    .First()
                    .similarityDescriptor;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public void ObtainProjectEstimates(xtmProjectDescriptorAPI project)
        {
            try
            {
                var result = GetWebService().obtainProjectEstimates(Configuration.LoginAPI, project,
                    new xtmObtainProjectEstimatesOptionsAPI());

                Console.WriteLine($"Project estimates:");
                var estimates = result.projectEstimates;

                Console.WriteLine($"Currency: {estimates.currency}");
                Console.WriteLine($"Price: {estimates.price}");
                Console.WriteLine($"Tax price: {estimates.taxPrice}");
                Console.WriteLine($"Tax rate: {estimates.taxRate}");
                Console.WriteLine($"Delivery date: {estimates.deliveryDate}");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }



        public void DownloadProjectMetricsURL(xtmProjectDescriptorAPI project)
        {
            try
            {
                var result = GetWebService().downloadProjectMetricsURL(Configuration.LoginAPI, project,
                    new xtmDownloadProjectMetricsURLOptionsAPI());
                WebClient client = new WebClient();
                client.DownloadFile(result.projectMetrics.fileURL, Configuration.DownloadDirectory + $"project_{project.id}_metrics.zip");
                Console.WriteLine($"Project metrics FILE downloaded from: {result.projectMetrics.fileURL}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void DownloadProjectMetricsMTOM(xtmProjectDescriptorAPI project)
        {
            try
            {
                var result = GetWebService().downloadProjectMetricsMTOM(Configuration.LoginAPI, project,
                    new xtmDownloadProjectMetricsMTOMOptionsAPI());
                var destinationPath = Configuration.DownloadDirectory + result.projectMetrics.fileName;
                File.WriteAllBytes(destinationPath, result.projectMetrics.fileMTOM);

                Console.WriteLine($"Succesfully downloaded project metric to {destinationPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void ObtainProjectStatistics(xtmProjectDescriptorAPI project)
        {
            try
            {
                var projectStatistics = GetWebService().obtainProjectStatistics(Configuration.LoginAPI, project,
                    new xtmObtainProjectStatisticsOptionsAPI()).ToList();
                Console.WriteLine("Obtained project statistics");
                foreach (var statisticsEntry in projectStatistics)
                {
                    Console.WriteLine("- Target language: " + statisticsEntry.targetLanguage);
                    statisticsEntry.userStatisticsAPI.ToList().ForEach(us =>
                    {
                        Console.WriteLine("-- User id: " + us.userDescriptor.id);
                        us.stepStatistics.ToList().ForEach(stpStat =>
                        {
                            Console.WriteLine("--- Step: " + stpStat.stepDescriptor.localizedWorkflowStepName);
                            stpStat.jobsStatistics.ToList().ForEach(jobStat =>
                            {

                                Console.WriteLine("--- Job id: " + jobStat.jobDescriptor.id);
                                Console.WriteLine("---- Words done: " + jobStat.statistics.wordsDone);
                                Console.WriteLine("---- Machine translation words: " + jobStat.statistics.machineTranslationWords);
                                Console.WriteLine("---- Total character count: " + jobStat.statistics.totalCharacterCount);
                            });
                        });
                    });

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void ObtainProjectAllStatistics(xtmProjectDescriptorAPI project)
        {
            try
            {
                var projectStatistics = GetWebService().obtainProjectAllStatistics(Configuration.LoginAPI, project, null);
                if (projectStatistics.languageStatistics != null)
                {
                    Console.WriteLine("Obtained project statistics");
                    foreach (var langeuageStatistics in projectStatistics.languageStatistics)
                    {
                        Console.WriteLine("- Target language: " + langeuageStatistics.targetLanguage);
                        langeuageStatistics.stepStatistics.ToList().ForEach(stpStat =>
                        {
                            Console.WriteLine("--- Step: " + stpStat.stepDescriptor.localizedWorkflowStepName);
                            Console.WriteLine("---- Words done: " + stpStat.sourceStatistics.wordsDone);
                            Console.WriteLine("---- Machine translation words: " + stpStat.sourceStatistics.machineTranslationWords);
                            Console.WriteLine("---- Total character count: " + stpStat.sourceStatistics.totalCharacterCount);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
