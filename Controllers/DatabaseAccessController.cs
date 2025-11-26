using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityReportWeb.Database.Models;
using System;
using System.Threading.Tasks;

namespace SecurityReportWeb.Controllers
{
    /// <summary>
    /// 提供外部測試資料庫可訪問性的 API 端點
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseAccessController : ControllerBase
    {
        private readonly ReportDbContext _context;

        public DatabaseAccessController(ReportDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 測試資料庫連接狀態
        /// </summary>
        /// <returns>資料庫連接狀態資訊</returns>
        [HttpGet("health")]
        public async Task<IActionResult> GetDatabaseHealth()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var isHealthy = canConnect;

                var response = new
                {
                    Status = isHealthy ? "Healthy" : "Unhealthy",
                    CanConnect = canConnect,
                    Timestamp = DateTime.UtcNow,
                    DatabaseName = _context.Database.GetDbConnection().Database
                };

                if (isHealthy)
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(503, response);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    Status = "Unhealthy",
                    CanConnect = false,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// 驗證資料庫結構完整性
        /// </summary>
        /// <returns>資料庫結構驗證結果</returns>
        [HttpGet("structure-validation")]
        public async Task<IActionResult> ValidateDatabaseStructure()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return StatusCode(503, new { Error = "無法連接到資料庫" });
                }

                // 測試每個資料表是否可訪問
                var urlListsAccessible = await TestTableAccessAsync(() => _context.UrlLists.AnyAsync());
                var zapreportsAccessible = await TestTableAccessAsync(() => _context.Zapreports.AnyAsync());
                var zapalertDetailsAccessible = await TestTableAccessAsync(() => _context.ZapalertDetails.AnyAsync());
                var riskDescriptionsAccessible = await TestTableAccessAsync(() => _context.RiskDescriptions.AnyAsync());
                var auditLogsAccessible = await TestTableAccessAsync(() => _context.AuditLogs.AnyAsync());

                var allTablesAccessible = urlListsAccessible && zapreportsAccessible && 
                                         zapalertDetailsAccessible && riskDescriptionsAccessible && 
                                         auditLogsAccessible;

                var validationResults = new
                {
                    Timestamp = DateTime.UtcNow,
                    Status = allTablesAccessible ? "Valid" : "Invalid",
                    Tables = new
                    {
                        UrlLists = new
                        {
                            Exists = true,
                            Accessible = urlListsAccessible
                        },
                        Zapreports = new
                        {
                            Exists = true,
                            Accessible = zapreportsAccessible
                        },
                        ZapalertDetails = new
                        {
                            Exists = true,
                            Accessible = zapalertDetailsAccessible
                        },
                        RiskDescriptions = new
                        {
                            Exists = true,
                            Accessible = riskDescriptionsAccessible
                        },
                        AuditLogs = new
                        {
                            Exists = true,
                            Accessible = auditLogsAccessible
                        }
                    }
                };

                return Ok(validationResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "Invalid",
                    Error = "結構驗證時發生錯誤",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// 測試資料表訪問性
        /// </summary>
        private async Task<bool> TestTableAccessAsync(Func<Task<bool>> testAction)
        {
            try
            {
                await testAction();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

