using ServiceReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Utilities
{
    class XTMAPIHelper
    {
        private XTMCustomerMTOMWebServiceClient webService;
        protected XTMAPIConfiguration Configuration { get; set; }

        public XTMAPIHelper(XTMAPIConfiguration configuration)
        {
            Configuration = configuration;
        }

        public XTMCustomerMTOMWebServiceClient GetWebService()
        {
            if (webService == null)
            {
                webService = new XTMCustomerMTOMWebServiceClient();
            }
            //webService.
            return webService;
        }

        public List<xtmJobDescriptorAPI> GetJobsDescriptors(List<xtmJobResponseAPI> jobs)
        {
            var jobsDescriptors = jobs.Select(x => x.jobDescriptor).ToList();
            return jobsDescriptors;
        }

        public List<xtmFileDescriptorAPI> GetFileDescriptors(List<xtmJobFileBaseResponseAPI> jobFileBaseResponse)
        {
            var result = jobFileBaseResponse.Select(x => x.fileDescriptor).ToList();
            return result;
        }
    }
}
