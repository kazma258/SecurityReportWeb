using SecurityReportWeb.Database.Dtos;

namespace SecurityReportWeb.Services;

/// <summary>
/// 認證服務介面
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 驗證使用者登入
    /// </summary>
    /// <param name="request">登入請求</param>
    /// <returns>登入回應</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// 驗證 JWT Token
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <returns>使用者資訊，如果 Token 無效則返回 null</returns>
    Task<UserInfoDto?> ValidateTokenAsync(string token);
}

