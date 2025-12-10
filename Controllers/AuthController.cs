using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecurityReportWeb.Database.Dtos;
using SecurityReportWeb.Services;

namespace SecurityReportWeb.Controllers;

/// <summary>
/// 認證相關 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="request">登入請求</param>
    /// <returns>登入回應（包含 JWT Token）</returns>
    /// <response code="200">登入成功</response>
    /// <response code="400">請求參數錯誤</response>
    /// <response code="401">帳號或密碼錯誤</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new LoginResponseDto
            {
                Success = false,
                ErrorMessage = "請求參數驗證失敗"
            });
        }

        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 驗證 Token 並取得使用者資訊
    /// </summary>
    /// <returns>使用者資訊</returns>
    /// <response code="200">Token 有效，返回使用者資訊</response>
    /// <response code="401">Token 無效或已過期</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoDto>> ValidateToken()
    {
        // 從 Header 中取得 Token
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { message = "未提供 Token" });
        }

        var userInfo = await _authService.ValidateTokenAsync(token);

        if (userInfo == null)
        {
            return Unauthorized(new { message = "Token 無效或已過期" });
        }

        return Ok(userInfo);
    }
}

