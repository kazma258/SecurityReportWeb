using System;

namespace SecurityReportWeb.Database.Models;

/// <summary>
/// 使用者模型
/// 用途：儲存系統使用者的基本資訊和認證資料
/// </summary>
public partial class User
{
    /// <summary>
    /// 使用者唯一識別碼（主鍵）
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 使用者帳號（唯一）
    /// 用途：用於登入系統的使用者名稱
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// 密碼雜湊值
    /// 用途：儲存經過雜湊處理的密碼，不儲存明文密碼
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// 使用者電子郵件
    /// 用途：用於通知、密碼重設等功能
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 使用者全名
    /// 用途：顯示在使用者介面上的名稱
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// 是否啟用
    /// 用途：控制使用者是否可以登入系統
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 建立時間（UTC）
    /// 用途：記錄使用者帳號建立的時間
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最後登入時間（UTC）
    /// 用途：記錄使用者最後一次登入的時間
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 導航屬性：使用者的角色關聯
    /// 用途：EF Core 關聯查詢，可透過此屬性存取使用者的所有角色
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

