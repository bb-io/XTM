using Blackbird.Applications.Sdk.Utils.Sdk.DataSourceHandlers;

namespace Apps.XTM.DataSourceHandlers.EnumHandlers;

public class FileTypeDataHandler : EnumDataHandler
{
    protected override Dictionary<string, string> EnumValues => new()
    {
        { "TARGET", "Target" },
        { "XLIFF", "XLIFF" },
        { "XLIFF_NTP", "XLIFF NTP" },
        { "QA_REPORT", "QA report" },
        { "HTML", "HTML" },
        { "HTML_TABLE", "HTML table" },
        { "PDF", "PDF" },
        { "PDF_TABLE", "PDF table" },
        { "TIPP", "TIPP" },
        { "HTML_EXTENDED_TABLE", "HTML extended table" },
        { "HTML_COLOURED", "HTML coloured" },
        { "HTML_COLOURED_BY_MATCH_RATE", "HTML coloured by match rate" },
        { "PDF_EXTENDED_TABLE", "PDF extended table" },
        { "PDF_COLOURED", "PDF coloured" },
        { "PDF_COLOURED_BY_XLIFF_DOC_STATUS", "PDF coloured by XLIFF doc status" },
        { "PDF_COLOURED_BY_MATCH_RATE", "PDF coloured by match rate" },
        { "TARGET_COLOURED_BY_MATCH_RATE", "Target coloured by match rate" },
        { "TARGET_COLOURED_BY_XLIFF_DOC_STATUS", "Target coloured by XLIFF doc status" },
        { "XLIFF_DOC", "XLIFF doc" },
        { "LQA_REPORT", "LQA report" },
        { "LQA_EXTENDED_TABLE_REPORT", "LQA extended table report" },
        { "TARGET_PSEUDO", "Target pseudo" },
        { "MULTI_EXCEL", "Multi Excel" },
        { "EXCEL_EXTENDED_TABLE", "Excel extended table" }
    };
}