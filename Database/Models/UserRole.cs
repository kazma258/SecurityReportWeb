namespace SecurityReportWeb.Database.Models;

/// <summary>
/// 使用者角色關聯模型
/// 用途：建立使用者與角色之間的多對多關聯
/// </summary>
public partial class UserRole
{
    /// <summary>
    /// 使用者角色關聯唯一識別碼（主鍵）
    /// </summary>
    public int UserRoleId { get; set; }

    /// <summary>
    /// 使用者 ID（外鍵）
    /// 用途：關聯到 User 表
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 角色 ID（外鍵）
    /// 用途：關聯到 Role 表
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 導航屬性：關聯的使用者
    /// 用途：EF Core 關聯查詢，可透過此屬性存取完整的使用者資訊
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 導航屬性：關聯的角色
    /// 用途：EF Core 關聯查詢，可透過此屬性存取完整的角色資訊
    /// </summary>
    public virtual Role Role { get; set; } = null!;
}

