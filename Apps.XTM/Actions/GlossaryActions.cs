using Apps.XTM.Invocables;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Apps.XTM.Models.Response.Glossaries;
using Apps.XTM.Models.Request.Glossaries;
using Apps.XTM.Constants;
using Apps.XTM.Extensions;
using TermService;


namespace Apps.XTM.Actions
{
    [ActionList]
    public class GlossaryActions : XtmInvocable
    {
        private readonly IFileManagementClient _fileManagementClient;

        public GlossaryActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
            : base(invocationContext)
        {
            _fileManagementClient = fileManagementClient;
        }

        [Action("Export glossary", Description = "Export glossary")]
        public async Task<ExportGlossaryResponse> ExportGlossary([ActionParameter] GlossaryRequest request)
        {
            var userId = Creds.Get(CredsNames.UserId);

            var loginApi = new TermService.loginAPI
            {
                client = Creds.Get(CredsNames.Client),
                userIdSpecified = true,
                userId = ParseId(userId),
                password = Creds.Get(CredsNames.Password),
            };
            var result = await TermWebServiceClient.exportTermAsync(loginApi, new xtmExportTermAPI[] {
                new xtmExportTermAPI() { 
                    fileType = xtmTermFileTypeEnum.TBX, fileTypeSpecified = true,
                    statusSpecified = false,
                    allLanguagesSpecified = false,
                    mainLanguageSpecified = false,
                    //customer = new TermService.xtmCustomerDescriptorAPI(){ id = ParseId(userId), externalIdSpecified = false, idSpecified = true, name = "BlackBeard"},
                    //domain = new xtmTermDomainAPI(){A}
                }
            }, new xtmExportTermOptionsAPI());
            var res = string.Join(',', result.@return.Select(x => x.fileName));

            //var export = TermWebServiceClient.exportTermAsync(loginApi, ).Result;
            //export.
            //ProjectManagerMTOClient.fil
            //ProjectManagerMTOClient.get
            //ProjectManagerMTOClient.downloadTMURLAsync(loginApi, )

            //var glossaryDetails = await Client.GetGlossaryAsync(request.GlossaryId);
            //var glossaryEntries = await Client.GetGlossaryEntriesAsync(request.GlossaryId);
            //var entries = glossaryEntries.ToDictionary();

            //var conceptEntries = new List<GlossaryConceptEntry>();
            //int counter = 0;
            //foreach (var entry in entries)
            //{
            //    var glossaryTermSection1 = new List<GlossaryTermSection>();
            //    glossaryTermSection1.Add(new GlossaryTermSection(entry.Key));

            //    var glossaryTermSection2 = new List<GlossaryTermSection>();
            //    glossaryTermSection2.Add(new GlossaryTermSection(entry.Value));

            //    var languageSections = new List<GlossaryLanguageSection>();
            //    languageSections.Add(new GlossaryLanguageSection(glossaryDetails.SourceLanguageCode, glossaryTermSection1));
            //    languageSections.Add(new GlossaryLanguageSection(glossaryDetails.TargetLanguageCode, glossaryTermSection2));

            //    conceptEntries.Add(new GlossaryConceptEntry(counter.ToString(), languageSections));
            //    ++counter;
            //}
            //var blackbirdGlossary = new Glossary(conceptEntries);
            //blackbirdGlossary.Title = glossaryDetails.Name;
            //using var stream = blackbirdGlossary.ConvertToTBX();
            return new ExportGlossaryResponse() { Filenames = res };
        }
    }
}
