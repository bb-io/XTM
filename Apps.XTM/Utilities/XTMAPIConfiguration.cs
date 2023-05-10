using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceReference;

namespace Apps.XTM.Utilities
{
    public class XTMAPIConfiguration
    {
        public string DownloadDirectory { get; set; } = @"C:\apiExample\";

        public loginAPI LoginAPI { get; set; }
        //{
        //    userId = 20,
        //    userIdSpecified = true,
        //    client = "abcd1234",
        //    password = "pass1234"

        //};

        public xtmCustomerDescriptorAPI Customer { get; set; } = new xtmCustomerDescriptorAPI()
        {
            idSpecified = true,
            id = 3109
            //or
            //name = ""
        };

        public xtmUserDescriptorAPI Linguist { get; set; } = new xtmUserDescriptorAPI()
        {
            idSpecified = true,
            id = 3096
            //or
            //name = ""
        };

        public languageCODE SourceLanguage { get; set; } = languageCODE.nl_NL;

        public languageCODE?[] TargetLanguages { get; set; } = new languageCODE?[]
        {
            languageCODE.en_GB,
            languageCODE.es_ES,
            languageCODE.en_US
        };

        public List<Uri> Translations { get; set; } = new List<Uri>
        {
            new Uri("http://www.w3schools.com/xml/simple.xml")
        };

        public List<Uri> TranslationsToUpdate { get; set; } = new List<Uri>
        {
            new Uri("http://www.w3schools.com/xml/note.xml")
        };

        public List<Uri> ReferenceFiles { get; set; } = new List<Uri>
        {
            new Uri("http://www.w3schools.com/xml/simple.xml")
        };
    }
}
