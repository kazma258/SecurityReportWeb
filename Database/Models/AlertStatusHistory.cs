using System;

namespace SecurityReportWeb.Database.Models;

/// <summary>
/// 警報狀態變更歷史記錄
/// 用途：記錄每次警報狀態變更的詳細資訊，用於審計追蹤和趨勢分析
/// </summary>
public partial class AlertStatusHistory
{
    /// <summary>
    /// 狀態變更歷史記錄的唯一識別碼（主鍵）
    /// </summary>
    public int HistoryId { get; set; }

    /// <summary>
    /// 關聯的警報 ID（外鍵）
    /// 用途：識別此狀態變更屬於哪個警報
    /// </summary>
    public int AlertId { get; set; }

    /// <summary>
    /// 狀態變更前的狀態值
    /// 用途：記錄狀態變更前的狀態，用於追蹤狀態變化歷程
    /// 可為 null：首次建立警報時沒有舊狀態
    /// </summary>
    public string? OldStatus { get; set; }

    /// <summary>
    /// 狀態變更後的狀態值
    /// 用途：記錄狀態變更後的目標狀態
    /// 必填：每次狀態變更都必須有明確的新狀態
    /// </summary>
    public string NewStatus { get; set; } = null!;

    /// <summary>
    /// 狀態變更時的說明或備註
    /// 用途：記錄負責人更新狀態時填寫的修復說明、處理方式等資訊
    /// 可為 null：允許不填寫備註
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 狀態變更的時間戳記（UTC 時間）
    /// 用途：記錄狀態變更的準確時間，用於時間序列分析和審計追蹤
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 執行狀態變更的使用者名稱
    /// 用途：記錄是誰執行了狀態變更，用於責任追蹤和權限審計
    /// 通常為負責人（Manager）的姓名
    /// </summary>
    public string UpdatedBy { get; set; } = null!;

    /// <summary>
    /// 執行狀態變更的使用者角色
    /// 用途：記錄執行者的角色類型，用於權限分析和審計
    /// 可能值：'Manager'（負責人）、'Supervisor'（主管）、'Admin'（系統管理員）
    /// </summary>
    public string UpdatedByRole { get; set; } = null!;

    /// <summary>
    /// 導航屬性：關聯的警報詳情
    /// 用途：EF Core 關聯查詢，可透過此屬性存取完整的警報資訊
    /// </summary>
    public virtual ZapalertDetail Alert { get; set; } = null!;
}

