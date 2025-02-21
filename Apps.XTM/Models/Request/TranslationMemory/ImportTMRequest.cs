using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.XTM.DataSourceHandlers;
using Apps.XTM.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.XTM.Models.Request.TranslationMemory
{
    public class ImportTMRequest
    {
        [Display("Customer ID")]
        [DataSource(typeof(CustomerDataHandler))]
        public string CustomerId { get; set; }

        [Display("File")]
        public FileReference File{ get; set; }

        [Display("Project name")]
        public string ImportProjectName { get; set; }

        [Display("Source language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string SourceLanguage { get; set; }

        [Display("Target language")]
        [DataSource(typeof(LanguageDataHandler))]
        public string TargetLanguage { get; set; }

        [Display("TM status")]
        [DataSource(typeof(TmStatusDataHandler))]
        public string? TmStatus { get; set; } 

        [Display("TM status import type")]
        [DataSource(typeof(TmStatusImportTypeDataHandler))]
        public string? TmStatusImportType { get; set; } 

        [Display("Whitespaces formatting type")]
        [DataSource(typeof(WhitespacesFormattingTypeDataHandler))]
        public string? WhitespacesFormattingType { get; set; } 

        [Display("Alt-Trans elements import")]
        [DataSource(typeof(AltTransElementsImportDataHandler))]
        public string? AltTransElementsImport { get; set; }

        [Display("Segments import type")]
        [DataSource(typeof(SegmentsImportTypeDataHandler))]
        public string? SegmentsImportType { get; set; }

        [Display("Bilingual terminology action")]
        [DataSource(typeof(BilingualTerminologyActionDataHandler))]
        public string? BilingualTerminologyAction { get; set; }

        [Display("Tag group IDs")]
        public string? TagGroupIds { get; set; }

        [Display("Tag IDs")]
        public IEnumerable<string>? TagIds { get; set; }
    }
}
