using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public DataController(ReportDbContext context)
        {
            _context = context;
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
                var items = await query
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
                        ReportCount = includeStats ? u.Zapreports.Count : null,
                        AlertCount = includeStats ? u.ZapalertDetails.Count : null
                    })
                    .ToListAsync();

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

                var totalCount = await query.CountAsync();
                var resolvedCount = await query.CountAsync(a => a.Status == "Closed");
                var unresolvedCount = totalCount - resolvedCount;
                var highRiskCount = await query.CountAsync(a => a.Level == "High" && a.Status != "Closed");

                var overallFixRate = totalCount > 0 ? (double)resolvedCount / totalCount * 100 : 0.0;

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
                return StatusCode(500, new { error = "取得儀表板總覽統計時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得風險等級分布
        /// </summary>
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

                var distribution = await query
                    .GroupBy(a => a.Level)
                    .Select(g => new { Level = g.Key, Count = g.Count() })
                    .ToListAsync();

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
                return StatusCode(500, new { error = "取得風險等級分布時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得歷次掃描結果比較
        /// </summary>
        [HttpGet("dashboard/scan-comparison")]
        public async Task<ActionResult<List<ScanComparisonDto>>> GetScanComparison(
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate,
            [FromQuery] string groupBy = "day",
            [FromQuery] string? unitName = null)
        {
            try
            {
                if (fromDate > toDate)
                {
                    return BadRequest(new { error = "起始日期不能大於結束日期" });
                }

                var query = _context.ZapalertDetails
                    .Include(a => a.RootUrl)
                    .Where(a => a.ReportDay >= fromDate && a.ReportDay <= toDate)
                    .AsQueryable();

                // 單位過濾
                if (!string.IsNullOrWhiteSpace(unitName))
                {
                    query = query.Where(a => a.RootUrl.UnitName == unitName);
                }

                List<ScanComparisonDto> result;

                if (groupBy.ToLower() == "week")
                {
                    // 按週分組
                    var alertsByWeek = await query
                        .GroupBy(a => new
                        {
                            Year = a.ReportDay.Year,
                            Week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                a.ReportDay.ToDateTime(TimeOnly.MinValue),
                                System.Globalization.CalendarWeekRule.FirstDay,
                                DayOfWeek.Monday)
                        })
                        .Select(g => new
                        {
                            Date = g.Key,
                            NewCount = g.Count(),
                            ResolvedCount = 0 // 需要從狀態歷史表查詢
                        })
                        .ToListAsync();

                    result = alertsByWeek.Select(a => new ScanComparisonDto
                    {
                        Date = $"{a.Date.Year}-W{a.Date.Week:D2}",
                        NewCount = a.NewCount,
                        ResolvedCount = a.ResolvedCount
                    }).ToList();
                }
                else if (groupBy.ToLower() == "month")
                {
                    // 按月分組
                    var alertsByMonth = await query
                        .GroupBy(a => new { Year = a.ReportDay.Year, Month = a.ReportDay.Month })
                        .Select(g => new
                        {
                            Date = g.Key,
                            NewCount = g.Count(),
                            ResolvedCount = 0 // 需要從狀態歷史表查詢
                        })
                        .ToListAsync();

                    result = alertsByMonth.Select(a => new ScanComparisonDto
                    {
                        Date = $"{a.Date.Year}-{a.Date.Month:D2}",
                        NewCount = a.NewCount,
                        ResolvedCount = a.ResolvedCount
                    }).ToList();
                }
                else
                {
                    // 按日分組（預設）
                    var alertsByDay = await query
                        .GroupBy(a => a.ReportDay)
                        .Select(g => new
                        {
                            Date = g.Key,
                            NewCount = g.Count(),
                            ResolvedCount = 0 // 需要從狀態歷史表查詢
                        })
                        .OrderBy(a => a.Date)
                        .ToListAsync();

                    result = alertsByDay.Select(a => new ScanComparisonDto
                    {
                        Date = a.Date.ToString("yyyy-MM-dd"),
                        NewCount = a.NewCount,
                        ResolvedCount = a.ResolvedCount
                    }).ToList();
                }

                // 查詢每日修復數量（從狀態歷史表）
                if (result.Any())
                {
                    var dateRange = result.Select(r => DateOnly.Parse(r.Date)).ToList();
                    var resolvedByDate = await _context.AlertStatusHistories
                        .Where(h => h.NewStatus == "Closed" && 
                                    h.UpdatedAt.Date >= fromDate.ToDateTime(TimeOnly.MinValue) &&
                                    h.UpdatedAt.Date <= toDate.ToDateTime(TimeOnly.MinValue))
                        .GroupBy(h => DateOnly.FromDateTime(h.UpdatedAt.Date))
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .ToListAsync();

                    foreach (var item in result)
                    {
                        var date = DateOnly.Parse(item.Date);
                        item.ResolvedCount = resolvedByDate.FirstOrDefault(r => r.Date == date)?.Count ?? 0;
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "取得歷次掃描結果比較時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得部門資安績效
        /// </summary>
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
        [HttpPatch("zap-alerts/{alertId}/status")]
        public async Task<ActionResult<UpdateAlertStatusResponseDto>> UpdateAlertStatus(
            int alertId,
            [FromBody] UpdateAlertStatusRequestDto request)
        {
            try
            {
                // 驗證狀態值
                var validStatuses = new[] { "Open", "In Progress", "Closed", "False Positive" };
                if (!validStatuses.Contains(request.Status))
                {
                    return BadRequest(new { error = "無效的狀態值", message = $"狀態必須為: {string.Join(", ", validStatuses)}" });
                }

                var alert = await _context.ZapalertDetails
                    .Include(a => a.RootUrl)
                    .FirstOrDefaultAsync(a => a.AlertId == alertId);

                if (alert == null)
                {
                    return NotFound(new { error = "找不到指定的警報" });
                }

                // TODO: 實作權限檢查
                // 目前暫時允許所有請求，後續需要加入身份驗證和權限檢查
                // var currentUser = GetCurrentUser();
                // if (currentUser.Role != "Admin" && currentUser.Name != alert.RootUrl.Manager)
                // {
                //     return StatusCode(403, new { error = "無權限更新此警報狀態" });
                // }

                var oldStatus = alert.Status;
                alert.Status = request.Status;

                // 記錄狀態變更歷史
                var history = new AlertStatusHistory
                {
                    AlertId = alertId,
                    OldStatus = oldStatus,
                    NewStatus = request.Status,
                    Remark = request.Remark,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System", // TODO: 從身份驗證取得實際使用者
                    UpdatedByRole = "Manager" // TODO: 從身份驗證取得實際角色
                };

                _context.AlertStatusHistories.Add(history);
                await _context.SaveChangesAsync();

                var result = new UpdateAlertStatusResponseDto
                {
                    AlertId = alertId,
                    Status = alert.Status,
                    UpdatedAt = history.UpdatedAt,
                    UpdatedBy = history.UpdatedBy
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "更新警報狀態時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得修復紀錄（單一警報）
        /// </summary>
        [HttpGet("zap-alerts/{alertId}/status-history")]
        public async Task<ActionResult<List<AlertStatusHistoryDto>>> GetAlertStatusHistory(int alertId)
        {
            try
            {
                var alert = await _context.ZapalertDetails.FindAsync(alertId);
                if (alert == null)
                {
                    return NotFound(new { error = "找不到指定的警報" });
                }

                // TODO: 實作權限檢查
                // 負責人可查看自己管理的警報歷史，主管可查看所有紀錄

                var history = await _context.AlertStatusHistories
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

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "取得修復紀錄時發生錯誤", message = ex.Message });
            }
        }

        /// <summary>
        /// 取得部門修復紀錄（主管用）
        /// </summary>
        [HttpGet("dashboard/fix-history")]
        public async Task<ActionResult<PagedResultDto<FixHistoryDto>>> GetFixHistory(
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

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.AlertStatusHistories
                    .Include(h => h.Alert)
                        .ThenInclude(a => a.RootUrl)
                    .Where(h => h.NewStatus == "Closed")
                    .AsQueryable();

                // 日期過濾
                if (fromDate.HasValue)
                {
                    var fromDateTime = fromDate.Value.ToDateTime(TimeOnly.MinValue);
                    query = query.Where(h => h.UpdatedAt >= fromDateTime);
                }

                if (toDate.HasValue)
                {
                    var toDateTime = toDate.Value.ToDateTime(TimeOnly.MaxValue);
                    query = query.Where(h => h.UpdatedAt <= toDateTime);
                }

                // 單位過濾
                if (!string.IsNullOrWhiteSpace(unitName))
                {
                    query = query.Where(h => h.Alert.RootUrl.UnitName == unitName);
                }

                // 負責人過濾
                if (!string.IsNullOrWhiteSpace(manager))
                {
                    query = query.Where(h => h.Alert.RootUrl.Manager == manager);
                }

                // 狀態過濾（雖然這裡只查 Closed，但保留參數以備擴展）
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(h => h.NewStatus == status);
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(h => h.UpdatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new FixHistoryDto
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

                var result = new PagedResultDto<FixHistoryDto>
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

