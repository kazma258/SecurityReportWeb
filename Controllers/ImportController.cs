using Microsoft.AspNetCore.Mvc;
using SecurityReportWeb.Import.Dtos;
using SecurityReportWeb.Import.Services;
using System;
using System.Threading.Tasks;

namespace SecurityReportWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly IImportService _importService;
        private readonly IHtmlParserService _htmlParserService;
        private readonly IXlsxParserService _xlsxParserService;

        public ImportController(IImportService importService, IHtmlParserService htmlParserService, IXlsxParserService xlsxParserService)
        {
            _importService = importService;
            _htmlParserService = htmlParserService;
            _xlsxParserService = xlsxParserService;
        }

        /// <summary>
        /// 上傳 HTML 檔案，自動解析並匯入資料庫
        /// </summary>
        [HttpPost("upload-html")]
        public async Task<ActionResult<ImportResultDto>> UploadHtml(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "未提供檔案或檔案為空" });
            }

            if (!file.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "僅支援 .html 檔案" });
            }

            try
            {
                // 讀取檔案內容
                string htmlContent;
                using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
                {
                    htmlContent = await reader.ReadToEndAsync();
                }

                // 解析 HTML
                var importRequest = await _htmlParserService.ParseZapReportAsync(htmlContent, file.FileName);

                // 匯入資料庫
                var result = await _importService.ImportAsync(importRequest);

                return Ok(new
                {
                    fileName = file.FileName,
                    fileSize = file.Length,
                    result
                });
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(501, new
                {
                    error = "HTML 解析功能尚未實作",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "處理檔案時發生錯誤",
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 直接提交已解析的 JSON 資料（用於測試或手動匯入）
        /// </summary>
        [HttpPost("json")]
        public async Task<ActionResult<ImportResultDto>> ImportJson([FromBody] ImportRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new 
                { 
                    error = "Request body is null or invalid JSON format.",
                    hint = "確保 JSON 格式正確，換行符號需轉義為 \\n，特殊字元需使用反斜線轉義。"
                });
            }

            try
            {
                var result = await _importService.ImportAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    error = "匯入時發生錯誤",
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 上傳 XLSX 檔案，解析 URL 清單並匯入資料庫
        /// </summary>
        [HttpPost("upload-xlsx")]
        public async Task<ActionResult<ImportResultDto>> UploadXlsx(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "未提供檔案或檔案為空" });
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "僅支援 .xlsx 檔案" });
            }

            try
            {
                // 解析 XLSX 檔案
                List<UrlListImportDto> urlList;
                using (var stream = file.OpenReadStream())
                {
                    urlList = await _xlsxParserService.ParseUrlListAsync(stream);
                }

                if (urlList == null || urlList.Count == 0)
                {
                    return BadRequest(new { error = "XLSX 檔案中沒有找到有效的 URL 清單資料" });
                }

                // 建立匯入請求
                var importRequest = new ImportRequestDto
                {
                    UrlLists = urlList,
                    RiskDescriptions = new List<RiskDescriptionImportDto>(),
                    ZapReports = new List<ZapReportImportDto>(),
                    ZapAlerts = new List<ZapAlertDetailImportDto>(),
                    ReplaceAlertsForSubmittedDays = false
                };

                // 匯入資料庫
                var result = await _importService.ImportAsync(importRequest);

                return Ok(new
                {
                    fileName = file.FileName,
                    fileSize = file.Length,
                    parsedUrlCount = urlList.Count,
                    result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "處理 XLSX 檔案時發生錯誤",
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 上傳 XLSX 檔案，僅解析並回傳 URL 清單（不匯入資料庫）
        /// </summary>
        [HttpPost("parse-xlsx")]
        public async Task<ActionResult<List<UrlListImportDto>>> ParseXlsx(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "未提供檔案或檔案為空" });
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "僅支援 .xlsx 檔案" });
            }

            try
            {
                // 解析 XLSX 檔案
                List<UrlListImportDto> urlList;
                using (var stream = file.OpenReadStream())
                {
                    urlList = await _xlsxParserService.ParseUrlListAsync(stream);
                }

                return Ok(new
                {
                    fileName = file.FileName,
                    fileSize = file.Length,
                    parsedUrlCount = urlList?.Count ?? 0,
                    data = urlList ?? new List<UrlListImportDto>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "解析 XLSX 檔案時發生錯誤",
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 上傳 HTML 檔案，僅解析並回傳結果（不匯入資料庫）
        /// </summary>
        [HttpPost("parse-html")]
        public async Task<ActionResult<ImportRequestDto>> ParseHtml(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "未提供檔案或檔案為空" });
            }

            if (!file.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "僅支援 .html 檔案" });
            }

            try
            {
                // 讀取檔案內容
                string htmlContent;
                using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
                {
                    htmlContent = await reader.ReadToEndAsync();
                }

                // 解析 HTML
                var importRequest = await _htmlParserService.ParseZapReportAsync(htmlContent, file.FileName);

                return Ok(new
                {
                    fileName = file.FileName,
                    fileSize = file.Length,
                    parsedData = new
                    {
                        urlListCount = importRequest.UrlLists?.Count ?? 0,
                        riskDescriptionCount = importRequest.RiskDescriptions?.Count ?? 0,
                        zapReportCount = importRequest.ZapReports?.Count ?? 0,
                        zapAlertCount = importRequest.ZapAlerts?.Count ?? 0,
                        replaceAlertsForSubmittedDays = importRequest.ReplaceAlertsForSubmittedDays
                    },
                    data = importRequest
                });
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(501, new
                {
                    error = "HTML 解析功能尚未實作",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "解析 HTML 檔案時發生錯誤",
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }
    }
}


