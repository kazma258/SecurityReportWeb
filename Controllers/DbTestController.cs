using Microsoft.AspNetCore.Mvc;
using SecurityReportWeb.Database.Models;
using System.Threading.Tasks;

namespace SecurityReportWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DbTestController : ControllerBase
    {
        private readonly ReportDbContext _context;

        public DbTestController(ReportDbContext context)
        {
            _context = context;
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (canConnect)
                {
                    return Ok("資料庫連線成功！");
                }
                else
                {
                    return StatusCode(500, "資料庫連線失敗。");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"資料庫連線時發生錯誤: {ex.Message}");
            }
        }
    }
}
