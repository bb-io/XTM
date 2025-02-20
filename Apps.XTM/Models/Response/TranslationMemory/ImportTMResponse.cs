using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Response.TranslationMemory
{
    public class ImportTMResponse
    {
        [Display("File ID")]
        public string FileId { get; set; }

        [Display("File name")]
        public string FileName { get; set; }
    }
}
