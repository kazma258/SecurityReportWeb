using System;
using System.Threading.Tasks;
using SecurityReportWeb.Import.Dtos;

namespace SecurityReportWeb.Import.Services;

/// <summary>
/// HTML 解析服務的佔位實作
/// 團隊成員完成實際解析邏輯後，可以替換此實作
/// </summary>
public class HtmlParserServiceStub : IHtmlParserService
{
    public Task<ImportRequestDto> ParseZapReportAsync(string htmlContent, string? fileName = null)
    {
        // TODO: 團隊成員實作實際的 HTML 解析邏輯
        // 此為示例佔位實作，返回空資料或拋出未實作例外
        
        throw new NotImplementedException(
            "HTML 解析邏輯尚未實作。" +
            "請在 HtmlParserService 中實作 ParseZapReportAsync 方法，" +
            "解析 HTML 內容並回傳 ImportRequestDto 物件。\n\n" +
            $"檔案名稱: {fileName ?? "(未提供)"}\n" +
            $"HTML 內容長度: {htmlContent?.Length ?? 0} 字元"
        );
        
        // 實際實作範例（團隊成員需要填入真實邏輯）：
        /*
        var result = new ImportRequestDto
        {
            UrlLists = new List<UrlListImportDto>(),
            RiskDescriptions = new List<RiskDescriptionImportDto>(),
            ZapReports = new List<ZapReportImportDto>(),
            ZapAlerts = new List<ZapAlertDetailImportDto>(),
            ReplaceAlertsForSubmittedDays = true
        };
        
        // 解析 HTML，提取站點資訊
        // result.UrlLists.Add(new UrlListImportDto { ... });
        
        // 解析風險描述
        // result.RiskDescriptions.Add(new RiskDescriptionImportDto { ... });
        
        // 解析報告資訊
        // result.ZapReports.Add(new ZapReportImportDto { ... });
        
        // 解析警告清單
        // result.ZapAlerts.Add(new ZapAlertDetailImportDto { ... });
        
        return Task.FromResult(result);
        */
    }
}

