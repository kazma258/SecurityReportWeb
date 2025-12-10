using System;

namespace SecurityReportWeb.Database.Models;

/// <summary>
/// 角色模型
/// 用途：定義系統中的角色類型，用於權限管理
/// </summary>
public partial class Role
{
    /// <summary>
    /// 角色唯一識別碼（主鍵）
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 角色名稱（唯一）
    /// 用途：角色的識別名稱，例如：Admin, Manager, User
    /// </summary>
    public string RoleName { get; set; } = null!;

    /// <summary>
    /// 角色顯示名稱
    /// 用途：在使用者介面上顯示的友好名稱
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 角色描述
    /// 用途：說明此角色的權限範圍和用途
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 建立時間（UTC）
    /// 用途：記錄角色建立的時間
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 導航屬性：擁有此角色的使用者關聯
    /// 用途：EF Core 關聯查詢，可透過此屬性存取擁有此角色的所有使用者
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

