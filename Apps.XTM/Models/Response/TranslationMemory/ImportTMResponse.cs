using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;

namespace Apps.XTM.Models.Response.TranslationMemory
{
    public class ImportTMResponse
    {
        [Display("File ID")]
        [JsonProperty("fileId")]
        public string FileId { get; set; }

        [Display("File name")]
        [JsonProperty("fileName")]
        public string FileName { get; set; }
    }

}
