using Blackbird.Applications.Sdk.Common;

namespace Apps.XTM.Models.Response
{
    public class StepResponse
    {
        public string Status { get; set; }
        [Display("Step name")]
        public string StepName { get; set; }
        [Display("Due to date")]
        public DateTime DueToDate { get; set; }
    }
}
