using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SecurityReportWeb.Database.Dtos;
using SecurityReportWeb.Database.Models;

namespace SecurityReportWeb.Services;

/// <summary>
/// 認證服務實作
/// </summary>
public class AuthService : IAuthService
{
    private readonly ReportDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ReportDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 驗證使用者登入
    /// </summary>
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        try
        {
            // 查詢使用者（包含角色）
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // 驗證使用者是否存在
            if (user == null)
            {
                _logger.LogWarning("登入失敗：使用者不存在 - {Username}", request.Username);
                return new LoginResponseDto
                {
                    Success = false,
                    ErrorMessage = "帳號或密碼錯誤"
                };
            }

            // 驗證使用者是否啟用
            if (!user.IsActive)
            {
                _logger.LogWarning("登入失敗：使用者已停用 - {Username}", request.Username);
                return new LoginResponseDto
                {
                    Success = false,
                    ErrorMessage = "帳號已被停用，請聯絡管理員"
                };
            }

            // 驗證密碼
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("登入失敗：密碼錯誤 - {Username}", request.Username);
                return new LoginResponseDto
                {
                    Success = false,
                    ErrorMessage = "帳號或密碼錯誤"
                };
            }

            // 更新最後登入時間
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 生成 JWT Token
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(24); // Token 24 小時後過期

            // 建立使用者資訊
            var userInfo = new UserInfoDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            };

            _logger.LogInformation("使用者登入成功 - {Username}", request.Username);

            return new LoginResponseDto
            {
                Success = true,
                Token = token,
                ExpiresAt = expiresAt,
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登入過程發生錯誤 - {Username}", request.Username);
            return new LoginResponseDto
            {
                Success = false,
                ErrorMessage = "登入過程發生錯誤，請稍後再試"
            };
        }
    }

    /// <summary>
    /// 驗證 JWT Token
    /// </summary>
    public async Task<UserInfoDto?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetJwtSecret());

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = GetJwtIssuer(),
                ValidateAudience = true,
                ValidAudience = GetJwtAudience(),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // 從 Token 中取得使用者 ID
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }

            // 查詢使用者資訊（包含角色）
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            if (user == null)
            {
                return null;
            }

            return new UserInfoDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(GetJwtSecret());
        var expires = DateTime.UtcNow.AddHours(24);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 加入使用者全名（如果有）
        if (!string.IsNullOrEmpty(user.FullName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FullName));
        }

        // 加入使用者角色
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = GetJwtIssuer(),
            Audience = GetJwtAudience(),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 驗證密碼
    /// </summary>
    private bool VerifyPassword(string password, string passwordHash)
    {
        return PasswordHelper.VerifyPassword(password, passwordHash);
    }

    /// <summary>
    /// 取得 JWT Secret（優先從環境變數讀取）
    /// </summary>
    /// <exception cref="InvalidOperationException">當 JWT Secret 未設定或長度不足時拋出</exception>
    private string GetJwtSecret()
    {
        // 優先從環境變數讀取
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
        
        // 如果環境變數不存在，才從配置檔案讀取
        if (string.IsNullOrEmpty(secret))
        {
            secret = _configuration["Jwt:Secret"];
        }
        
        // 驗證 Secret 是否有效
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogError("JWT_SECRET 環境變數或配置未設定");
            throw new InvalidOperationException(
                "JWT_SECRET 環境變數或配置未設定。請設定 JWT_SECRET 環境變數，且長度至少需要 16 個字元（128 位元）。");
        }
        
        if (secret.Contains("${"))
        {
            _logger.LogError("JWT Secret 包含佔位符，請設定實際的金鑰值");
            throw new InvalidOperationException(
                "JWT Secret 包含佔位符。請設定實際的 JWT_SECRET 環境變數值，且長度至少需要 16 個字元（128 位元）。");
        }
        
        if (secret.Length < 16)
        {
            _logger.LogError("JWT Secret 長度不足：{Length} 個字元，至少需要 16 個字元", secret.Length);
            throw new InvalidOperationException(
                $"JWT Secret 長度不足：目前為 {secret.Length} 個字元，至少需要 16 個字元（128 位元）。請設定足夠長的 JWT_SECRET 環境變數。");
        }
        
        return secret;
    }

    /// <summary>
    /// 取得 JWT Issuer（優先從環境變數讀取）
    /// </summary>
    private string GetJwtIssuer()
    {
        return Environment.GetEnvironmentVariable("JWT_ISSUER") 
            ?? _configuration["Jwt:Issuer"] 
            ?? "SecurityReportWeb";
    }

    /// <summary>
    /// 取得 JWT Audience（優先從環境變數讀取）
    /// </summary>
    private string GetJwtAudience()
    {
        return Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
            ?? _configuration["Jwt:Audience"] 
            ?? "SecurityReportWeb";
    }
}

