using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SecurityReportWeb.Import.Dtos;

namespace SecurityReportWeb.Import.Services;

public interface IXlsxParserService
{
    Task<List<UrlListImportDto>> ParseUrlListAsync(Stream xlsxStream);
}
