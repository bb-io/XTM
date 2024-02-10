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
using System.Xml.Serialization;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using DocumentFormat.OpenXml.Vml.Office;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using System.Net.Mime;
using System.Xml;


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

            var customerForExport = new TermService.xtmCustomerDescriptorAPI() 
            { 
                id = ParseId(request.CustomerId), 
                externalIdSpecified = false, 
                idSpecified = true 
            };
            var result = await TermWebServiceClient.exportTermAsync(loginApi, new xtmExportTermAPI[] {
                new xtmExportTermAPI() { 
                    fileType = xtmTermFileTypeEnum.TBX, 
                    fileTypeSpecified = true,
                    statusSpecified = true,
                    status = xtmTermStatusEnum.ALL,
                    allLanguages = true,
                    allLanguagesSpecified = true,
                    mainLanguageSpecified = false,
                    customer = customerForExport,
                    translationLanguages = request.Languages == null ? 
                    Enum.GetValues(typeof(TermService.languageCODE)).Cast<TermService.languageCODE?>().ToArray() :
                    request.Languages.Select(x => (TermService.languageCODE?)Enum.Parse(typeof(TermService.languageCODE?), x)).ToArray()
                }
            }, new xtmExportTermOptionsAPI());
            var fileDescriptors = result.@return.Select(x => new xtmTermBaseFileDescriptorAPI() { id = x.id, idSpecified = true }).ToArray();


            while (true)
            {
                var checkRes = await TermWebServiceClient.checkTermCompletionAsync(loginApi, fileDescriptors, new xtmCheckTermCompletionOptionsAPI());
                if (checkRes.@return.All(x => x.status == xtmTermCompletionStatusEnum.FINISHED))
                    break;
                await Task.Delay(1000);
            }

            var resultFile = await TermWebServiceClient.downloadTermMTOMAsync(loginApi, fileDescriptors, new xtmDownloadTermMTOMOptionsAPI());

            XmlSerializer serializer = new XmlSerializer(typeof(XTMBasicTbxDto));
            using var tbxFileStream = new MemoryStream(resultFile.@return.First().fileMTOM);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = null;
            settings.DtdProcessing = DtdProcessing.Ignore;

            using var reader = XmlReader.Create(tbxFileStream, settings);
            var tbxDto = (XTMBasicTbxDto)serializer.Deserialize(reader);
            

            var conceptEntries = new List<GlossaryConceptEntry>();
            int counter = 0;
            foreach (var termEntry in tbxDto.Text.Body.TermEntry)
            {
                var languageSections = new List<GlossaryLanguageSection>();
                foreach (var langSet in termEntry.LangSet)
                {
                    var glossaryTermSection = new List<GlossaryTermSection>();
                    glossaryTermSection.Add(new GlossaryTermSection(langSet.Ntig.TermGrp.Term));
                    languageSections.Add(new GlossaryLanguageSection(langSet.Lang, glossaryTermSection));
                }
                conceptEntries.Add(new GlossaryConceptEntry(counter.ToString(), languageSections));
                ++counter;
            }
            var blackbirdGlossary = new Glossary(conceptEntries);
            blackbirdGlossary.Title = resultFile.@return.First().fileName;
            using var stream = blackbirdGlossary.ConvertToTbx();
            return new ExportGlossaryResponse() { File = await _fileManagementClient.UploadAsync(stream, MediaTypeNames.Application.Xml, blackbirdGlossary.Title) };
        }

        [Action("Import glossary", Description = "Import glossary")]
        public async Task ImportGlossary([ActionParameter] ImportGlossaryRequest request)
        {
            if (request.File.Size > 7800000)
                throw new ArgumentException("Maximum file size for XTM glossary is 7.5 MB");

            using var glossaryStream = await _fileManagementClient.DownloadAsync(request.File);

            var blackbirdGlossary = await glossaryStream.ConvertFromTbx();

            var xmBasicTbxDto = new XTMBasicTbxDto() { Text = new() { Body = new() { TermEntry = new() } } };
            foreach (var entry in blackbirdGlossary.ConceptEntries)
            {
                var termEntry = new TermEntry() { LangSet = new List<LangSet>()};
                foreach (var languageSection in entry.LanguageSections)
                {
                    var langSet = new LangSet() { 
                        Lang = languageSection.LanguageCode,
                        Ntig = new Ntig() { 
                            TermGrp = new TermGrp() { 
                                Term = languageSection.Terms.First().Term 
                            } 
                        } 
                    };
                    termEntry.LangSet.Add(langSet);
                }
                xmBasicTbxDto.Text.Body.TermEntry.Add(termEntry);
            }

            var userId = Creds.Get(CredsNames.UserId);

            var loginApi = new TermService.loginAPI
            {
                client = Creds.Get(CredsNames.Client),
                userIdSpecified = true,
                userId = ParseId(userId),
                password = Creds.Get(CredsNames.Password),
            };
            using var ms = new MemoryStream();
            XmlSerializer serializer = new XmlSerializer(typeof(XTMBasicTbxDto));
            serializer.Serialize(ms, xmBasicTbxDto);

            var customerForExport = new TermService.xtmCustomerDescriptorAPI()
            {
                id = ParseId(request.CustomerId),
                externalIdSpecified = false,
                idSpecified = true
            };
            var fileArr = ms.ToArray();

            var resultFile = await TermWebServiceClient.importTermMTOMAsync(loginApi,
            new xtmImportTermMTOMAPI[] {
                new xtmImportTermMTOMAPI() {
                    fileMTOM = fileArr,
                    customer = new TermService.xtmCustomerDescriptorAPI()
                    {
                        id = ParseId(request.CustomerId),
                        externalIdSpecified = false,
                        idSpecified = true
                    },
                    fileName = request.File.Name,
                    fileType = xtmTermFileTypeEnum.TBX,
                    fileTypeSpecified = true
                }
            },
            new xtmImportTermMTOMOptionsAPI() { purgeTermsSpecified = false, addToExistingTermsSpecified = false }); //{ addToExistingTermsSpecified = false, purgeTermsSpecified = false}

            while (true)
            {
                var checkRes = await TermWebServiceClient.checkTermCompletionAsync(loginApi, resultFile.@return.Select(x => new xtmTermBaseFileDescriptorAPI() { id = x.id, idSpecified = true }).ToArray(), new xtmCheckTermCompletionOptionsAPI());
                if (checkRes.@return.All(x => x.status == xtmTermCompletionStatusEnum.FINISHED))
                    break;
                await Task.Delay(1000);
            }
        }
    }
}
