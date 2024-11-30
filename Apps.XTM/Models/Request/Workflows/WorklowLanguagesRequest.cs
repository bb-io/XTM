using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.XTM.Models.Request.Workflows
{
    public class WorklowLanguagesRequest
    {
        [Display("Target languages")]
        [DataSource(typeof(LanguageDataHandler))]
        public IEnumerable<string>? TargetLanguages { get; set; }
    }
}
