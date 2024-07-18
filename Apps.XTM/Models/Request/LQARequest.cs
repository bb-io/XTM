

using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request
{
    public class LQARequest
    {
        [Display("Date from")]
        public DateTime? DateFrom { get; set; }

        [Display("Date to")]
        public DateTime? DateTo { get; set; }

        [DataSource(typeof(LanguageDataHandler))]
        [Display("Target languages")]
        public List<string>? TargetLangs { get; set; }

        [DataSource(typeof(LqaTypeDataHandler))]
        [Display("LQA type")]
        public string? Type { get; set; }
    }
}
