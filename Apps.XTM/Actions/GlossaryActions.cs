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
using DocumentFormat.OpenXml.Office2016.Excel;
using Blackbird.Applications.Sdk.Common.Files;


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
            var result = await TermWebServiceClient.exportTermAsync(GetLoginAPIObj(), new xtmExportTermAPI[] {
                new xtmExportTermAPI() {
                    fileType = xtmTermFileTypeEnum.TBX,
                    fileTypeSpecified = true,
                    status = request.Status != null ? Enum.Parse<xtmTermStatusEnum>(request.Status) : xtmTermStatusEnum.ALL,
                    statusSpecified = true,
                    allLanguages = request.AllLanguages != null ? request.AllLanguages.Value : false,
                    allLanguagesSpecified = true,
                    mainLanguage = request.MainLanguage != null ? Enum.Parse<TermService.languageCODE>(request.MainLanguage) : default,
                    mainLanguageSpecified = request.MainLanguage != null,
                    customer = GetTermsCustomer(request.CustomerId),
                    domain = request.Domain != null ? new xtmTermDomainAPI(){ name = request.Domain } : default,
                    columnsToExport = request.Columns != null ? request.Columns.Select(x => (xtmTermColumnsEnum?)Enum.Parse<xtmTermColumnsEnum>(x)).ToArray() : default,
                    translationLanguages = request.Languages == null ?
                    Enum.GetValues(typeof(TermService.languageCODE)).Cast<TermService.languageCODE?>().ToArray() :
                    request.Languages.Select(x => (TermService.languageCODE?)Enum.Parse<TermService.languageCODE>(x)).ToArray()
                }
            }, new xtmExportTermOptionsAPI()) ;

            var fileDescriptors = result.@return.Select(x => new xtmTermBaseFileDescriptorAPI() { id = x.id, idSpecified = true }).ToArray();
            await PollTermFileOperationStatus(fileDescriptors);

            var resultFile = await TermWebServiceClient.downloadTermMTOMAsync(GetLoginAPIObj(), fileDescriptors, new xtmDownloadTermMTOMOptionsAPI());
            return new ExportGlossaryResponse() { File = await XTMToBlackbirdGlossary(resultFile.@return.First()) };
        }

        [Action("Import glossary", Description = "Import glossary")]
        public async Task ImportGlossary([ActionParameter] ImportGlossaryRequest request)
        {
            var xtmGlossary = await BlackbirdToXTMGlossary(request.File);
            if (xtmGlossary.Length > 7800000)
                throw new ArgumentException("Maximum XTM glossary file size is 7.5 MB.\nPlease provide smaller file");

            var resultFile = await TermWebServiceClient.importTermMTOMAsync(GetLoginAPIObj(),
            new xtmImportTermMTOMAPI[] {
                new xtmImportTermMTOMAPI() {
                    fileMTOM = xtmGlossary,
                    customer = GetTermsCustomer(request.CustomerId),
                    fileName = request.File.Name,
                    fileType = xtmTermFileTypeEnum.TBX,
                    fileTypeSpecified = true
                }
            },
            new xtmImportTermMTOMOptionsAPI() { 
                purgeTerms = request.PurgeTerms != null ? request.PurgeTerms.Value : default,
                purgeTermsSpecified = request.PurgeTerms != null,
                addToExistingTerms = request.AddToExisting != null ? request.AddToExisting.Value : default,
                addToExistingTermsSpecified = request.AddToExisting != null
            });

            var fileDescriptors = resultFile.@return.Select(x => new xtmTermBaseFileDescriptorAPI() { id = x.id, idSpecified = true }).ToArray();
            await PollTermFileOperationStatus(fileDescriptors);
        }

        private TermService.xtmCustomerDescriptorAPI GetTermsCustomer(string customerId)
        {
            return new TermService.xtmCustomerDescriptorAPI()
            {
                id = ParseId(customerId),
                externalIdSpecified = false,
                idSpecified = true
            };
        }

        private async Task PollTermFileOperationStatus(xtmTermBaseFileDescriptorAPI[] fileDescriptors)
        {
            while (true)
            {
                var checkRes = await TermWebServiceClient.checkTermCompletionAsync(GetLoginAPIObj(), fileDescriptors, new xtmCheckTermCompletionOptionsAPI());
                if (checkRes.@return.First().status == xtmTermCompletionStatusEnum.FINISHED)
                    break;
                else if (checkRes.@return.First().status != xtmTermCompletionStatusEnum.IN_PROGRESS)
                    throw new ArgumentException($"Error during terms operation. Status: {checkRes.@return.First().status.ToString()}");
                await Task.Delay(1000);
            }
        }

        private async Task<FileReference> XTMToBlackbirdGlossary(xtmTermMTOMResponseAPI xtmGlossary)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XTMBasicTbxDto));
            using var tbxFileStream = new MemoryStream(xtmGlossary.fileMTOM);

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
            blackbirdGlossary.Title = xtmGlossary.fileName;
            using var stream = blackbirdGlossary.ConvertToTbx();
            return await _fileManagementClient.UploadAsync(stream, MediaTypeNames.Application.Xml, blackbirdGlossary.Title);
        }

        private async Task<byte[]> BlackbirdToXTMGlossary(FileReference blackbirdGlossary)
        {
            using var glossaryStream = await _fileManagementClient.DownloadAsync(blackbirdGlossary);

            var blackbirdGlossaryObj = await glossaryStream.ConvertFromTbx();

            var xmBasicTbxDto = new XTMBasicTbxDto() { Text = new() { Body = new() { TermEntry = new() } } };
            foreach (var entry in blackbirdGlossaryObj.ConceptEntries)
            {
                var termEntry = new TermEntry() { LangSet = new List<LangSet>() };
                foreach (var languageSection in entry.LanguageSections)
                {
                    var langSet = new LangSet()
                    {
                        Lang = languageSection.LanguageCode,
                        Ntig = new Ntig()
                        {
                            TermGrp = new TermGrp()
                            {
                                Term = languageSection.Terms.First().Term
                            }
                        }
                    };
                    termEntry.LangSet.Add(langSet);
                }
                xmBasicTbxDto.Text.Body.TermEntry.Add(termEntry);
            }

            using var ms = new MemoryStream();
            XmlSerializer serializer = new XmlSerializer(typeof(XTMBasicTbxDto));
            serializer.Serialize(ms, xmBasicTbxDto);
            return ms.ToArray();
        }
    }
}
