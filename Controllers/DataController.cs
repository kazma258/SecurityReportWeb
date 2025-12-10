using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecurityReportWeb.Database.Constants;
using SecurityReportWeb.Database.Dtos;
using SecurityReportWeb.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurityReportWeb.Controllers
{
    /// <summary>
    /// 提供資料庫資料查詢的 API 端點
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly ReportDbContext _context;
        private readonly ILogger<DataController> _logger;

        public DataController(ReportDbContext context, ILogger<DataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 取得 URL 清單（支援分頁、搜尋、排序）
        /// </summary>
        /// <param name="pageNumber">頁碼（從 1 開始）</param>
        /// <param name="pageSize">每頁筆數（預設 20，最大 100）</param>
        /// <param name="search">搜尋關鍵字（搜尋 WebName、Url、UnitName、Manager）</param>
        /// <param name="unitName">依單位名稱過濾</param>
        /// <param name="manager">依管理者過濾</param>
        /// <param name="sortBy">排序欄位（webName, url, unitName, uploadDate）</param>
        /// <param name="sortOrder">排序方向（asc, desc）</param>
        /// <param name="includeStats">是否包含統計資訊（報告數、警報數）</param>
        [HttpGet("url-lists")]
        public async Task<ActionResult<PagedResultDto<UrlListDto>>> GetUrlLists(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? unitName = null,
            [FromQuery] string? manager = null,
            [FromQuery] string sortBy = "webName",
            [FromQuery] string sortOrder = "asc",
            [FromQuery] bool includeStats = false)
        {
            try
            {
                // 驗證參數
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.UrlLists.AsQueryable();

                // 搜尋
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(u => 
                        u.WebName.ToLower().Contains(searchLower) ||
                        u.Url.ToLower().Contains(searchLower) ||
                        (u.UnitName != null && u.UnitName.ToLower().Contains(searchLower)) ||
                        (u.Manager != null && u.Manager.ToLower().Contains(searchLower)));
                }

                // 過濾
                if (!string.IsNullOrWhiteSpace(unitName))
                {
                    query = query.Where(u => u.UnitName == unitName);
                }

                if (!string.IsNullOrWhiteSpace(manager))
                {
                    query = query.Where(u => u.Manager == manager);
                }

                // 排序
                query = sortBy.ToLower() switch
                {
                    "url" => sortOrder.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.Url) 
                        : query.OrderBy(u => u.Url),
                    "unitname" => sortOrder.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.UnitName) 
                        : query.OrderBy(u => u.UnitName),
                    "uploaddate" => sortOrder.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.UploadDate) 
                        : query.OrderBy(u => u.UploadDate),
                    _ => sortOrder.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.WebName) 
                        : query.OrderBy(u => u.WebName)
                };

                // 取得總數
                var totalCount = await query.CountAsync();

                // 分頁
                IQueryable<UrlListDto> itemsQuery;
                
                if (includeStats)
                {
                    // 當需要統計資訊時，使用子查詢來獲取計數
                    itemsQuery = query
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(u => new UrlListDto
                        {
                            UrlId = u.UrlId,
                            Url = u.Url,
                            Ip = u.Ip,
                            WebName = u.WebName,
                            UnitName = u.UnitName,
                            Remark = u.Remark,
                            Manager = u.Manager,
                            ManagerMail = u.ManagerMail,
                            OutsourcedVendor = u.OutsourcedVendor,
                            RiskReportLink = u.RiskReportLink,
                            UploadDate = u.UploadDate,
                            ReportCount = u.Zapreports.Count(),
                            AlertCount = u.ZapalertDetails.Count()
                        });
                }
                else
                {
                    // 不需要統計資訊時，直接查詢
                    itemsQuery = query
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(u => new UrlListDto
                        {
                            UrlId = u.UrlId,
                            Url = u.Url,
                            Ip = u.Ip,
                            WebName = u.WebName,
                            UnitName = u.UnitName,
                            Remark = u.Remark,
                            Manager = u.Manager,
                            ManagerMail = u.ManagerMail,
                            OutsourcedVendor = u.OutsourcedVendor,
                            RiskReportLink = u.RiskReportLink,
                            UploadDate = u.UploadDate,
                            ReportCount = null,
                            AlertCount = null
                        });
                }

                var items = await itemsQuery.ToListAsync();

                var result = new PagedResultDto<UrlListDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "查詢 URL 清單時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得單一 URL 清單詳情
        /// </summary>
        [HttpGet("url-lists/{id}")]
        public async Task<ActionResult<UrlListDto>> GetUrlList(Guid id, [FromQuery] bool includeStats = true)
        {
            try
            {
                var urlList = await _context.UrlLists
                    .Where(u => u.UrlId == id)
                    .Select(u => new UrlListDto
                    {
                        UrlId = u.UrlId,
                        Url = u.Url,
                        Ip = u.Ip,
                        WebName = u.WebName,
                        UnitName = u.UnitName,
                        Remark = u.Remark,
                        Manager = u.Manager,
                        ManagerMail = u.ManagerMail,
                        OutsourcedVendor = u.OutsourcedVendor,
                        RiskReportLink = u.RiskReportLink,
                        UploadDate = u.UploadDate,
                        ReportCount = includeStats ? u.Zapreports.Count : null,
                        AlertCount = includeStats ? u.ZapalertDetails.Count : null
                    })
                    .FirstOrDefaultAsync();

                if (urlList == null)
                {
                    return NotFound(new { error = "找不到指定的 URL 清單" });
                }

                return Ok(urlList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "查詢 URL 清單時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得 ZAP 報告列表（支援分頁、過濾、排序）
        /// </summary>
        [HttpGet("zap-reports")]
        public async Task<ActionResult<PagedResultDto<ZapReportDto>>> GetZapReports(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? siteUrlId = null,
            [FromQuery] string? siteWebName = null,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null,
            [FromQuery] bool? isDeleted = null,
            [FromQuery] string sortBy = "generatedDate",
            [FromQuery] string sortOrder = "desc",
            [FromQuery] bool includeStats = false)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Zapreports
                    .Include(r => r.SiteUrl)
                    .AsQueryable();

                // 過濾
                if (siteUrlId.HasValue)
                {
                    query = query.Where(r => r.SiteUrlId == siteUrlId.Value);
                }

                if (!string.IsNullOrWhiteSpace(siteWebName))
                {
                    query = query.Where(r => r.SiteUrl.WebName == siteWebName);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(r => r.GeneratedDay >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(r => r.GeneratedDay <= toDate.Value);
                }

                if (isDeleted.HasValue)
                {
                    query = query.Where(r => r.IsDeleted == isDeleted.Value);
                }

                // 排序
                query = sortBy.ToLower() switch
                {
                    "generatedday" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(r => r.GeneratedDay)
                        : query.OrderByDescending(r => r.GeneratedDay),
                    "zapversion" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(r => r.Zapversion)
                        : query.OrderByDescending(r => r.Zapversion),
                    _ => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(r => r.GeneratedDate)
                        : query.OrderByDescending(r => r.GeneratedDate)
                };

                var totalCount = await query.CountAsync();

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ZapReportDto
                    {
                        ReportId = r.ReportId,
                        SiteUrlId = r.SiteUrlId,
                        SiteWebName = r.SiteUrl.WebName,
                        SiteUrl = r.SiteUrl.Url,
                        GeneratedDate = r.GeneratedDate,
                        GeneratedDay = r.GeneratedDay,
                        Zapversion = r.Zapversion,
                        Zapsupporter = r.Zapsupporter,
                        IsDeleted = r.IsDeleted,
                        AlertCount = includeStats ? r.ZapalertDetails.Count : null
                    })
                    .ToListAsync();

                var result = new PagedResultDto<ZapReportDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "查詢 ZAP 報告時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得 ZAP 警報詳情列表（支援分頁、過濾、排序）
        /// </summary>
        [HttpGet("zap-alerts")]
        public async Task<ActionResult<PagedResultDto<ZapAlertDetailDto>>> GetZapAlerts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? rootUrlId = null,
            [FromQuery] string? rootWebName = null,
            [FromQuery] string? riskName = null,
            [FromQuery] string? level = null,
            [FromQuery] string? status = null,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null,
            [FromQuery] string sortBy = "reportDate",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.ZapalertDetails
                    .Include(a => a.RootUrl)
                    .AsQueryable();

                // 過濾
                if (rootUrlId.HasValue)
                {
                    query = query.Where(a => a.RootUrlId == rootUrlId.Value);
                }

                if (!string.IsNullOrWhiteSpace(rootWebName))
                {
                    query = query.Where(a => a.RootUrl.WebName == rootWebName);
                }

                if (!string.IsNullOrWhiteSpace(riskName))
                {
                    query = query.Where(a => a.RiskName == riskName);
                }

                if (!string.IsNullOrWhiteSpace(level))
                {
                    query = query.Where(a => a.Level == level);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(a => a.Status == status);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.ReportDay >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.ReportDay <= toDate.Value);
                }

                // 排序
                query = sortBy.ToLower() switch
                {
                    "riskname" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(a => a.RiskName)
                        : query.OrderByDescending(a => a.RiskName),
                    "level" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(a => a.Level)
                        : query.OrderByDescending(a => a.Level),
                    "status" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(a => a.Status)
                        : query.OrderByDescending(a => a.Status),
                    "reportday" => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(a => a.ReportDay)
                        : query.OrderByDescending(a => a.ReportDay),
                    _ => sortOrder.ToLower() == "asc"
                        ? query.OrderBy(a => a.ReportDate)
                        : query.OrderByDescending(a => a.ReportDate)
                };

                var totalCount = await query.CountAsync();

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new ZapAlertDetailDto
                    {
                        AlertId = a.AlertId,
                        RootUrlId = a.RootUrlId,
                        RootWebName = a.RootUrl.WebName,
                        RootUrl = a.RootUrl.Url,
                        Url = a.Url,
                        ReportDate = a.ReportDate,
                        ReportDay = a.ReportDay,
                        RiskName = a.RiskName,
                        Level = a.Level,
                        Method = a.Method,
                        Parameter = a.Parameter,
                        Attack = a.Attack,
                        Evidence = a.Evidence,
                        Status = a.Status,
                        OtherInfo = a.OtherInfo
                    })
                    .ToListAsync();

                var result = new PagedResultDto<ZapAlertDetailDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "查詢 ZAP 警報時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得風險描述列表
        /// </summary>
        [HttpGet("risk-descriptions")]
        public async Task<ActionResult<List<RiskDescriptionDto>>> GetRiskDescriptions(
            [FromQuery] string? search = null,
            [FromQuery] string? name = null)
        {
            try
            {
                var query = _context.RiskDescriptions.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(r => 
                        r.Name.ToLower().Contains(searchLower) ||
                        (r.Description != null && r.Description.ToLower().Contains(searchLower)));
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    query = query.Where(r => r.Name == name);
                }

                var items = await query
                    .OrderBy(r => r.Name)
                    .Select(r => new RiskDescriptionDto
                    {
                        RiskId = r.RiskId,
                        Name = r.Name,
                        Description = r.Description,
                        Solution = r.Solution,
                        Reference = r.Reference,
                        Cweid = r.Cweid,
                        Wascid = r.Wascid,
                        PluginId = r.PluginId,
                        Signature = r.Signature
                    })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "查詢風險描述時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得統計資訊（用於儀表板）
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            try
            {
                var stats = new
                {
                    TotalUrlLists = await _context.UrlLists.CountAsync(),
                    TotalZapReports = await _context.Zapreports.CountAsync(r => !r.IsDeleted),
                    TotalZapAlerts = await _context.ZapalertDetails.CountAsync(),
                    TotalRiskDescriptions = await _context.RiskDescriptions.CountAsync(),
                    AlertsByLevel = await _context.ZapalertDetails
                        .GroupBy(a => a.Level)
                        .Select(g => new { Level = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    AlertsByStatus = await _context.ZapalertDetails
                        .GroupBy(a => a.Status)
                        .Select(g => new { Status = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    TopRiskNames = await _context.ZapalertDetails
                        .GroupBy(a => a.RiskName)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .Select(g => new { RiskName = g.Key, Count = g.Count() })
                        .ToListAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "取得統計資訊時發生錯誤", message = ex.Message });
            }
        }

        #region 儀表板 API

        /// <summary>
        /// 取得儀表板總覽統計
        /// </summary>
        /// <param name="fromDate">起始日期 (YYYY-MM-DD)</param>
        /// <param name="toDate">結束日期 (YYYY-MM-DD)</param>
        /// <param name="unitName">依單位名稱過濾</param>
        /// <returns>儀表板總覽統計資料</returns>
        [HttpGet("dashboard/overview")]
        public async Task<ActionResult<DashboardOverviewDto>> GetDashboardOverview(
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null,
            [FromQuery] string? unitName = null)
        {
            try
            {
                var query = _context.ZapalertDetails
                    .Include(a => a.RootUrl)
                    .AsQueryable();

                // 日期過濾
                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.ReportDay >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.ReportDay <= toDate.Value);
                }

                // 單位過濾
                if (!string.IsNullOrWhiteSpace(unitName))
                {
                    query = query.Where(a => a.RootUrl.UnitName == unitName);
                }

                // 尚未解決的漏洞數量（Status != "Closed"）
                var unresolvedCount = await query
                    .Where(a => a.Status != "Closed")
                    .CountAsync();

                // 已修復的漏洞數量（Status == "Closed"）
                var resolvedCount = await query
                    .Where(a => a.Status == "Closed")
                    .CountAsync();

                // 總數量
                var totalCount = unresolvedCount + resolvedCount;

                // 整體修復率（百分比）
                var overallFixRate = totalCount > 0
                    ? (double)resolvedCount / totalCount * 100
                    : 0.0;

                // 高風險漏洞數量（Level == "High" 且 Status != "Closed"）
                var highRiskCount = await query
                    .Where(a => a.Level == "High" && a.Status != "Closed")
                    .CountAsync();

                var result = new DashboardOverviewDto
                {
                    UnresolvedCount = unresolvedCount,
                    ResolvedCount = resolvedCount,
                    OverallFixRate = Math.Round(overallFixRate, 2),
                    HighRiskCount = highRiskCount
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得儀表板總覽統計時發生錯誤");
                return StatusCode(500, new
                {
                    error = "取得儀表板總覽統計時發生錯誤",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// 取得風險等級分布
        /// </summary>
        /// <param name="fromDate">起始日期 (YYYY-MM-DD)</param>
        /// <param name="toDate">結束日期 (YYYY-MM-DD)</param>
        /// <param name="unitName">依單位名稱過濾</param>
        /// <returns>風險等級分布資料</returns>
        [HttpGet("dashboard/risk-level-distribution")]
        public async Task<ActionResult<RiskLevelDistributionDto>> GetRiskLevelDistribution(
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null,
            [FromQuery] string? unitName = null)
        {
            try
            {
                var query = _context.ZapalertDetails
                    .Include(a => a.RootUrl)
                    .AsQueryable();

                // 日期過濾
                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.ReportDay >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.ReportDay <= toDate.Value);
                }

                // 單位過濾
                if (!string.IsNullOrWhiteSpace(unitName))
                {
                    query = query.Where(a => a.RootUrl.UnitName == unitName);
                }

                // 使用 GroupBy 查詢各風險等級的數量
                var distribution = await query
                    .GroupBy(a => a.Level)
                    .Select(g => new { Level = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 建立回應 DTO
                var result = new RiskLevelDistributionDto
                {
                    High = distribution.FirstOrDefault(d => d.Level == "High")?.Count ?? 0,
                    Medium = distribution.FirstOrDefault(d => d.Level == "Medium")?.Count ?? 0,
                    Low = distribution.FirstOrDefault(d => d.Level == "Low")?.Count ?? 0,
                    Informational = distribution.FirstOrDefault(d => d.Level == "Informational")?.Count ?? 0
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得風險等級分布時發生錯誤");
                return StatusCode(500, new
                {
                    error = "取得風險等級分布時發生錯誤",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// 取得歷次掃描結果比較
        /// </summary>
        /// <param name="fromDate">起始日期 (YYYY-MM-DD)</param>
        /// <param name="toDate">結束日期 (YYYY-MM-DD)</param>
        /// <param name="groupBy">分組方式：day, week, month (預設: day)</param>
        /// <param name="unitName">依單位名稱過濾</param>
        /// <returns>掃描結果比較資料</returns>
        [HttpGet("dashboard/scan-comparison")]
        public async Task<ActionResult<List<ScanComparisonDto>>> GetScanComparison(
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate,
            [FromQuery] string groupBy = "day",
            [FromQuery] string? unitName = null)
        {
            try
            {
                // 驗證參數
                if (fromDate > toDate)
                {
                    return BadRequest(new { 
                        error = "日期範圍無效", 
                        message = "起始日期不能大於結束日期" 
                    });
                }

                // 驗證 groupBy 參數
                var validGroupBy = new[] { "day", "week", "month" };
                if (!validGroupBy.Contains(groupBy.ToLower()))
                {
                    return BadRequest(new { 
                        error = "無效的分組方式", 
                        message = "groupBy 必須為：day, week, month" 
                    });
                }

                // 建立基礎查詢
                var alertQuery = _context.ZapalertDetails
                    .Include(a => a.RootUrl)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(unitName))
                {
                    alertQuery = alertQuery.Where(a => a.RootUrl.UnitName == unitName);
                }

                // 計算新增數量（依 ReportDay）
                var newCounts = await alertQuery
                    .Where(a => a.ReportDay >= fromDate && a.ReportDay <= toDate)
                    .GroupBy(a => a.ReportDay)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 計算修復數量（依 AlertStatusHistory.UpdatedAt）
                var resolvedQuery = _context.AlertStatusHistories
                    .Where(h => h.NewStatus == "Closed" && 
                                h.UpdatedAt.Date >= fromDate.ToDateTime(TimeOnly.MinValue) &&
                                h.UpdatedAt.Date <= toDate.ToDateTime(TimeOnly.MinValue))
                    .AsQueryable();

                // 如果指定了單位，需要過濾修復記錄
                if (!string.IsNullOrWhiteSpace(unitName))
                {
                    resolvedQuery = resolvedQuery
                        .Include(h => h.Alert)
                            .ThenInclude(a => a.RootUrl)
                        .Where(h => h.Alert.RootUrl.UnitName == unitName);
                }

                var resolvedCounts = await resolvedQuery
                    .GroupBy(h => DateOnly.FromDateTime(h.UpdatedAt.Date))
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 根據 groupBy 參數分組
                var result = new List<ScanComparisonDto>();

                if (groupBy.ToLower() == "day")
                {
                    // 按天分組
                    var allDates = new HashSet<DateOnly>();
                    newCounts.ForEach(n => allDates.Add(n.Date));
                    resolvedCounts.ForEach(r => allDates.Add(r.Date));

                    // 確保日期範圍內的所有日期都被包含
                    for (var date = fromDate; date <= toDate; date = date.AddDays(1))
                    {
                        allDates.Add(date);
                    }

                    foreach (var date in allDates.OrderBy(d => d))
                    {
                        var newCount = newCounts.FirstOrDefault(n => n.Date == date)?.Count ?? 0;
                        var resolvedCount = resolvedCounts.FirstOrDefault(r => r.Date == date)?.Count ?? 0;

                        result.Add(new ScanComparisonDto
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            NewCount = newCount,
                            ResolvedCount = resolvedCount
                        });
                    }
                }
                else if (groupBy.ToLower() == "week")
                {
                    // 按週分組
                    var newCountsByWeek = newCounts
                        .GroupBy(n => GetWeekStart(n.Date))
                        .Select(g => new { WeekStart = g.Key, Count = g.Sum(x => x.Count) })
                        .ToList();

                    var resolvedCountsByWeek = resolvedCounts
                        .GroupBy(r => GetWeekStart(r.Date))
                        .Select(g => new { WeekStart = g.Key, Count = g.Sum(x => x.Count) })
                        .ToList();

                    // 取得所有週的開始日期
                    var allWeekStarts = new HashSet<DateOnly>();
                    newCountsByWeek.ForEach(n => allWeekStarts.Add(n.WeekStart));
                    resolvedCountsByWeek.ForEach(r => allWeekStarts.Add(r.WeekStart));

                    // 確保日期範圍內的所有週都被包含
                    for (var date = fromDate; date <= toDate; date = date.AddDays(1))
                    {
                        allWeekStarts.Add(GetWeekStart(date));
                    }

                    foreach (var weekStart in allWeekStarts.OrderBy(w => w))
                    {
                        var weekEnd = weekStart.AddDays(6);
                        var newCount = newCountsByWeek.FirstOrDefault(n => n.WeekStart == weekStart)?.Count ?? 0;
                        var resolvedCount = resolvedCountsByWeek.FirstOrDefault(r => r.WeekStart == weekStart)?.Count ?? 0;

                        result.Add(new ScanComparisonDto
                        {
                            Date = $"{weekStart:yyyy-MM-dd} ~ {weekEnd:yyyy-MM-dd}",
                            NewCount = newCount,
                            ResolvedCount = resolvedCount
                        });
                    }
                }
                else if (groupBy.ToLower() == "month")
                {
                    // 按月分組
                    var newCountsByMonth = newCounts
                        .GroupBy(n => new { n.Date.Year, n.Date.Month })
                        .Select(g => new { 
                            Year = g.Key.Year, 
                            Month = g.Key.Month, 
                            Count = g.Sum(x => x.Count) 
                        })
                        .ToList();

                    var resolvedCountsByMonth = resolvedCounts
                        .GroupBy(r => new { r.Date.Year, r.Date.Month })
                        .Select(g => new { 
                            Year = g.Key.Year, 
                            Month = g.Key.Month, 
                            Count = g.Sum(x => x.Count) 
                        })
                        .ToList();

                    // 取得所有月份
                    var allMonths = new HashSet<(int Year, int Month)>();
                    newCountsByMonth.ForEach(n => allMonths.Add((n.Year, n.Month)));
                    resolvedCountsByMonth.ForEach(r => allMonths.Add((r.Year, r.Month)));

                    // 確保日期範圍內的所有月份都被包含
                    for (var date = fromDate; date <= toDate; date = date.AddMonths(1))
                    {
                        allMonths.Add((date.Year, date.Month));
                    }

                    foreach (var month in allMonths.OrderBy(m => m.Year).ThenBy(m => m.Month))
                    {
                        var newCount = newCountsByMonth.FirstOrDefault(n => n.Year == month.Year && n.Month == month.Month)?.Count ?? 0;
                        var resolvedCount = resolvedCountsByMonth.FirstOrDefault(r => r.Year == month.Year && r.Month == month.Month)?.Count ?? 0;

                        result.Add(new ScanComparisonDto
                        {
                            Date = $"{month.Year}-{month.Month:D2}",
                            NewCount = newCount,
                            ResolvedCount = resolvedCount
                        });
                    }
                }

                return Ok(result.OrderBy(r => r.Date).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "取得歷次掃描結果比較時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得週的開始日期（週一）
        /// </summary>
        private static DateOnly GetWeekStart(DateOnly date)
        {
            var dayOfWeek = date.DayOfWeek;
            var daysToSubtract = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
            return date.AddDays(-daysToSubtract);
        }

        /// <summary>
        /// 取得部門資安績效
        /// </summary>
        /// <param name="fromDate">起始日期 (YYYY-MM-DD)</param>
        /// <param name="toDate">結束日期 (YYYY-MM-DD)</param>
        /// <param name="sortBy">排序欄位：totalCount, fixRate (預設: totalCount)</param>
        /// <param name="sortOrder">排序方向：asc, desc (預設: desc)</param>
        /// <returns>部門資安績效資料</returns>
        [HttpGet("dashboard/department-performance")]
        public async Task<ActionResult<List<DepartmentPerformanceDto>>> GetDepartmentPerformance(
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null,
            [FromQuery] string sortBy = "totalCount",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                var query = _context.ZapalertDetails
                    .Include(a => a.RootUrl)
                    .AsQueryable();

                // 日期過濾
                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.ReportDay >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.ReportDay <= toDate.Value);
                }

                var departmentStats = await query
                    .GroupBy(a => new
                    {
                        a.RootUrl.UnitName,
                        a.RootUrl.Manager
                    })
                    .Select(g => new DepartmentPerformanceDto
                    {
                        UnitName = g.Key.UnitName,
                        Manager = g.Key.Manager,
                        HighRiskCount = g.Count(a => a.Level == "High"),
                        MediumRiskCount = g.Count(a => a.Level == "Medium"),
                        LowRiskCount = g.Count(a => a.Level == "Low"),
                        TotalCount = g.Count(),
                        ResolvedCount = g.Count(a => a.Status == "Closed"),
                        FixRate = 0.0 // 稍後計算
                    })
                    .ToListAsync();

                // 計算修復率
                foreach (var dept in departmentStats)
                {
                    dept.FixRate = dept.TotalCount > 0 
                        ? Math.Round((double)dept.ResolvedCount / dept.TotalCount * 100, 2) 
                        : 0.0;
                }

                // 排序
                if (sortBy.ToLower() == "fixrate")
                {
                    departmentStats = sortOrder.ToLower() == "asc"
                        ? departmentStats.OrderBy(d => d.FixRate).ToList()
                        : departmentStats.OrderByDescending(d => d.FixRate).ToList();
                }
                else
                {
                    departmentStats = sortOrder.ToLower() == "asc"
                        ? departmentStats.OrderBy(d => d.TotalCount).ToList()
                        : departmentStats.OrderByDescending(d => d.TotalCount).ToList();
                }

                return Ok(departmentStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "取得部門資安績效時發生錯誤", message = ex.Message });
            }
        }

        #endregion

        #region 修復狀況更新 API

        /// <summary>
        /// 更新警報狀態
        /// </summary>
        /// <param name="alertId">警報 ID</param>
        /// <param name="request">狀態更新請求</param>
        /// <returns>更新後的狀態資訊</returns>
        [HttpPatch("zap-alerts/{alertId}/status")]
        public async Task<ActionResult<AlertStatusUpdateResponseDto>> UpdateAlertStatus(
            int alertId,
            [FromBody] AlertStatusUpdateRequestDto request)
        {
            try
            {
                // 驗證請求參數
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 驗證狀態值
                if (!AlertStatus.IsValid(request.Status))
                {
                    return BadRequest(new { 
                        error = "無效的狀態值", 
                        message = $"狀態值必須為：{string.Join(", ", AlertStatus.AllStatuses)}" 
                    });
                }

                // 查詢警報
                var alert = await _context.ZapalertDetails
                    .Include(a => a.RootUrl)
                    .FirstOrDefaultAsync(a => a.AlertId == alertId);

                if (alert == null)
                {
                    return NotFound(new { error = "找不到指定的警報" });
                }

                // 記錄舊狀態
                var oldStatus = alert.Status;

                // 更新狀態（使用資料庫交易）
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 更新警報狀態
                    alert.Status = request.Status;
                    _context.ZapalertDetails.Update(alert);

                    // 記錄狀態變更歷史
                    var history = new AlertStatusHistory
                    {
                        AlertId = alertId,
                        OldStatus = oldStatus,
                        NewStatus = request.Status,
                        Remark = request.Remark,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = "System", // TODO: 未來改為實際使用者名稱
                        UpdatedByRole = "Manager" // TODO: 未來改為實際使用者角色
                    };

                    _context.AlertStatusHistories.Add(history);

                    // 儲存變更
                    await _context.SaveChangesAsync();

                    // 提交交易
                    await transaction.CommitAsync();

                    // 建立回應
                    var response = new AlertStatusUpdateResponseDto
                    {
                        AlertId = alertId,
                        Status = alert.Status,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = "System" // TODO: 未來改為實際使用者名稱
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "更新警報狀態時發生錯誤，AlertId: {AlertId}", alertId);
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "資料庫更新失敗，AlertId: {AlertId}", alertId);
                return StatusCode(500, new { 
                    error = "資料庫更新失敗", 
                    message = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新警報狀態時發生錯誤，AlertId: {AlertId}", alertId);
                return StatusCode(500, new { 
                    error = "更新警報狀態時發生錯誤", 
                    message = ex.Message 
                });
            }
        }

        /// <summary>
        /// 取得單一警報的狀態變更歷史記錄
        /// </summary>
        /// <param name="alertId">警報 ID</param>
        /// <returns>狀態變更歷史記錄列表</returns>
        [HttpGet("zap-alerts/{alertId}/status-history")]
        public async Task<ActionResult<List<AlertStatusHistoryDto>>> GetAlertStatusHistory(int alertId)
        {
            try
            {
                // 驗證警報是否存在
                var alertExists = await _context.ZapalertDetails
                    .AnyAsync(a => a.AlertId == alertId);

                if (!alertExists)
                {
                    return NotFound(new { error = "找不到指定的警報" });
                }

                // 查詢狀態歷史記錄
                var histories = await _context.AlertStatusHistories
                    .Where(h => h.AlertId == alertId)
                    .OrderByDescending(h => h.UpdatedAt)
                    .Select(h => new AlertStatusHistoryDto
                    {
                        HistoryId = h.HistoryId,
                        AlertId = h.AlertId,
                        OldStatus = h.OldStatus,
                        NewStatus = h.NewStatus,
                        Remark = h.Remark,
                        UpdatedAt = h.UpdatedAt,
                        UpdatedBy = h.UpdatedBy,
                        UpdatedByRole = h.UpdatedByRole
                    })
                    .ToListAsync();

                return Ok(histories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得警報狀態歷史時發生錯誤，AlertId: {AlertId}", alertId);
                return StatusCode(500, new { 
                    error = "取得警報狀態歷史時發生錯誤", 
                    message = ex.Message 
                });
            }
        }

        /// <summary>
        /// 取得部門修復紀錄（主管用）
        /// </summary>
        /// <param name="fromDate">起始日期 (YYYY-MM-DD)</param>
        /// <param name="toDate">結束日期 (YYYY-MM-DD)</param>
        /// <param name="unitName">依單位名稱過濾</param>
        /// <param name="manager">依負責人過濾</param>
        /// <param name="status">依狀態過濾</param>
        /// <param name="pageNumber">頁碼（預設: 1）</param>
        /// <param name="pageSize">每頁筆數（預設: 20，最大: 100）</param>
        /// <returns>修復紀錄分頁結果</returns>
        [HttpGet("dashboard/fix-history")]
        public async Task<ActionResult<PagedResultDto<FixHistoryItemDto>>> GetFixHistory(
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null,
            [FromQuery] string? unitName = null,
            [FromQuery] string? manager = null,
            [FromQuery] string? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // TODO: 實作權限檢查
                // 僅主管可存取此端點

                // 驗證參數
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                // 建立基礎查詢（查詢狀態為 Closed 的歷史記錄）
                var query = _context.AlertStatusHistories
                    .Where(h => h.NewStatus == "Closed")
                    .Include(h => h.Alert)
                        .ThenInclude(a => a.RootUrl)
                    .AsQueryable();

                // 套用過濾條件
                if (fromDate.HasValue)
                {
                    query = query.Where(h => h.UpdatedAt.Date >= fromDate.Value.ToDateTime(TimeOnly.MinValue));
                }

                if (toDate.HasValue)
                {
                    query = query.Where(h => h.UpdatedAt.Date <= toDate.Value.ToDateTime(TimeOnly.MinValue));
                }

                if (!string.IsNullOrWhiteSpace(unitName))
                {
                    query = query.Where(h => h.Alert.RootUrl.UnitName == unitName);
                }

                if (!string.IsNullOrWhiteSpace(manager))
                {
                    query = query.Where(h => h.Alert.RootUrl.Manager == manager);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(h => h.Alert.Status == status);
                }

                // 取得總數
                var totalCount = await query.CountAsync();

                // 分頁查詢
                var items = await query
                    .OrderByDescending(h => h.UpdatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new FixHistoryItemDto
                    {
                        AlertId = h.AlertId,
                        WebName = h.Alert.RootUrl.WebName,
                        UnitName = h.Alert.RootUrl.UnitName,
                        RiskName = h.Alert.RiskName,
                        Level = h.Alert.Level,
                        Status = h.NewStatus,
                        UpdatedAt = h.UpdatedAt,
                        UpdatedBy = h.UpdatedBy,
                        Remark = h.Remark
                    })
                    .ToListAsync();

                // 建立分頁回應
                var result = new PagedResultDto<FixHistoryItemDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "取得部門修復紀錄時發生錯誤", message = ex.Message });
            }
        }

        #endregion
    }
}

