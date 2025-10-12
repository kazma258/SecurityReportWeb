using System.Threading.Tasks;
using SecurityReportWeb.Import.Dtos;

namespace SecurityReportWeb.Import.Services;

/// <summary>
/// HTML 解析服務介面
/// 團隊成員需實作此介面，將上傳的 HTML 檔案解析為 ImportRequestDto
/// </summary>
public interface IHtmlParserService
{
    /// <summary>
    /// 解析 ZAP HTML 報告
    /// </summary>
    /// <param name="htmlContent">HTML 檔案內容</param>
    /// <param name="fileName">檔案名稱（選用，可用於日誌或錯誤訊息）</param>
    /// <returns>解析後的匯入請求物件</returns>
    Task<ImportRequestDto> ParseZapReportAsync(string htmlContent, string? fileName = null);
}

