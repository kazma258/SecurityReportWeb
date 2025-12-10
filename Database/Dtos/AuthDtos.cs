using System.ComponentModel.DataAnnotations;

namespace SecurityReportWeb.Database.Dtos;

/// <summary>
/// 登入請求 DTO
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// 使用者帳號
    /// </summary>
    [Required(ErrorMessage = "帳號為必填項目")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "帳號長度必須在 3 到 100 個字元之間")]
    public string Username { get; set; } = null!;

    /// <summary>
    /// 使用者密碼
    /// </summary>
    [Required(ErrorMessage = "密碼為必填項目")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度必須在 6 到 100 個字元之間")]
    public string Password { get; set; } = null!;
}

/// <summary>
/// 登入回應 DTO
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// 是否登入成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// JWT Token（僅在成功時提供）
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Token 過期時間（UTC）
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 使用者資訊（僅在成功時提供）
    /// </summary>
    public UserInfoDto? User { get; set; }

    /// <summary>
    /// 錯誤訊息（僅在失敗時提供）
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 使用者資訊 DTO
/// </summary>
public class UserInfoDto
{
    /// <summary>
    /// 使用者 ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 使用者帳號
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// 使用者全名
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// 電子郵件
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 使用者角色列表
    /// </summary>
    public List<string> Roles { get; set; } = new();
}

