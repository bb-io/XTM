﻿using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.XTM.Models.Response.Glossaries
{
    public class ExportGlossaryResponse
    {
        //[Display("Glossary")]
        //public FileReference File { get; set; }

        [Display("Test")]
        public string Filenames { get; set; }
    }
}
