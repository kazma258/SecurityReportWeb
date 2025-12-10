using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecurityReportWeb.Database.Dtos;
using SecurityReportWeb.Database.Models;
using SecurityReportWeb.Services;

namespace SecurityReportWeb.Controllers;

/// <summary>
/// 使用者管理 API 控制器（開發環境用於建立測試使用者）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ReportDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(ReportDbContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 建立測試使用者（僅供開發環境使用）
    /// </summary>
    /// <param name="username">使用者帳號</param>
    /// <param name="password">密碼</param>
    /// <param name="fullName">全名（可選）</param>
    /// <param name="email">電子郵件（可選）</param>
    /// <returns>建立結果</returns>
    [HttpPost("create-test-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateTestUser(
        [FromQuery] string username,
        [FromQuery] string password,
        [FromQuery] string? fullName = null,
        [FromQuery] string? email = null)
    {
        // 僅在開發環境允許
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (env != "Development" && env != "Development")
        {
            return BadRequest(new { message = "此端點僅在開發環境可用" });
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new { message = "帳號和密碼為必填項目" });
        }

        // 檢查使用者是否已存在
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (existingUser != null)
        {
            return BadRequest(new { message = "使用者已存在" });
        }

        // 建立新使用者
        var user = new User
        {
            Username = username,
            PasswordHash = PasswordHelper.HashPassword(password),
            FullName = fullName,
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("建立測試使用者成功 - {Username}", username);

        return Ok(new
        {
            message = "使用者建立成功",
            userId = user.UserId,
            username = user.Username
        });
    }

    /// <summary>
    /// 為使用者指派角色（僅供開發環境使用）
    /// </summary>
    /// <param name="username">使用者帳號</param>
    /// <param name="roleName">角色名稱</param>
    /// <returns>指派結果</returns>
    [HttpPost("assign-role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AssignRole(
        [FromQuery] string username,
        [FromQuery] string roleName)
    {
        // 僅在開發環境允許
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (env != "Development")
        {
            return BadRequest(new { message = "此端點僅在開發環境可用" });
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return NotFound(new { message = "使用者不存在" });
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == roleName);

        if (role == null)
        {
            // 如果角色不存在，自動建立
            role = new Role
            {
                RoleName = roleName,
                DisplayName = roleName,
                CreatedAt = DateTime.UtcNow
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        // 檢查是否已經有這個角色
        var existingUserRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.RoleId == role.RoleId);

        if (existingUserRole != null)
        {
            return BadRequest(new { message = "使用者已擁有此角色" });
        }

        // 指派角色
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("為使用者指派角色成功 - {Username} -> {RoleName}", username, roleName);

        return Ok(new
        {
            message = "角色指派成功",
            username = user.Username,
            roleName = role.RoleName
        });
    }
}

