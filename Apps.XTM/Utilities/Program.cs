using System;
using System.Threading;

namespace Apps.XTM.Utilities
{
    using ServiceReference;
    using System.Collections.Generic;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("XTM API example\n");

            XTMAPIScenarios scenarios = new XTMAPIScenarios(new XTMAPIConfiguration());

            scenarios.GetXTMInfo();
            scenarios.SupportedAndNotSupportedFilesInfo();
            scenarios.CreateTemplate();

            xtmProjectResponseAPI project = scenarios.CreateProjectMTOM();

            if (project != null)
            {
                Console.WriteLine("Please wait!");
                Thread.Sleep(2000);

                List<xtmJobDescriptorAPI> jobsDescriptors = scenarios.GetJobsDescriptors(project.jobs.ToList());

                xtmProjectDescriptorAPI projectDescriptor = project.projectDescriptor;

                scenarios.CheckProjectAnalysisCompletion(projectDescriptor);

                scenarios.AssignLinguistToJob(jobsDescriptors);

                scenarios.ObtainMetricsStatisticsAndCosts(projectDescriptor, jobsDescriptors);

                scenarios.StartProject(projectDescriptor);

                scenarios.UpdateJobStepProperty(jobsDescriptors.First());

                scenarios.MoveJobWorkflow(jobsDescriptors.First());

                scenarios.MoveProjectWorkflow(projectDescriptor);

                List<xtmJobFileBaseResponseAPI> jobFiles = scenarios.GenerateJobFile(jobsDescriptors);

                scenarios.CheckJobFileCompletion(scenarios.GetFileDescriptors(jobFiles));

                scenarios.DownloadJobFileURL(scenarios.GetFileDescriptors(jobFiles));

                scenarios.ObtainLinks(projectDescriptor, jobsDescriptors);

                scenarios.CheckProjectCompletion(projectDescriptor);
            }

            scenarios.FindUpdateAndDeleteProject(project.projectDescriptor);

            Console.WriteLine("\nPress Esc to exit.");

            while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;
        }
    }
}
