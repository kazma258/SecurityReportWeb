using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SecurityReportWeb.Import.Dtos;

namespace SecurityReportWeb.Import.Services;

public class XlsxParserService : IXlsxParserService
{
    public async Task<List<UrlListImportDto>> ParseUrlListAsync(Stream xlsxStream)
    {
        var urlList = new List<UrlListImportDto>();
        ExcelPackage.License.SetNonCommercialOrganization("NCUT_ComputerCenter");

        using var package = new ExcelPackage(xlsxStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            return urlList; // or throw an exception
        }

        var rowCount = worksheet.Dimension.Rows;
        var colCount = worksheet.Dimension.Columns;

        // Assumes first row is the header
        var headers = new List<string>();
        for (int col = 1; col <= colCount; col++)
        {
            headers.Add(worksheet.Cells[1, col].Text);
        }

        for (int row = 2; row <= rowCount; row++)
        {
            var urlListItem = new UrlListImportDto();
            for (int col = 1; col <= colCount; col++)
            {
                var header = headers[col - 1];
                var cellValue = worksheet.Cells[row, col].Text;

                switch (header)
                {
                    case "*對外網路IP位址":
                        urlListItem.Ip = cellValue;
                        break;
                    case "*名稱":
                        urlListItem.WebName = cellValue;
                        break;
                    case "*業務單位":
                        urlListItem.UnitName = cellValue;
                        break;
                    case "*網頁位址(URL/服務埠）":
                        urlListItem.Url = cellValue;
                        break;
                    case "備註與用途":
                        urlListItem.Remark = cellValue;
                        break;
                    case "管理人":
                        urlListItem.Manager = cellValue;
                        break;
                    case "管理人信箱":
                        urlListItem.ManagerMail = cellValue;
                        break;
                    case "*委外廠商":
                        urlListItem.OutsourcedVendor = cellValue;
                        break;
                    case "*風險報告":
                        urlListItem.RiskReportLink = cellValue;
                        break;
                }
            }
            urlList.Add(urlListItem);
        }

        return await Task.FromResult(urlList);
    }
}
