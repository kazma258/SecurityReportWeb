namespace SecurityReportWeb.Database.Constants;

/// <summary>
/// 警報狀態常數定義
/// </summary>
public static class AlertStatus
{
    /// <summary>
    /// 未處理（預設值）
    /// </summary>
    public const string Open = "Open";

    /// <summary>
    /// 處理中
    /// </summary>
    public const string InProgress = "In Progress";

    /// <summary>
    /// 已修復
    /// </summary>
    public const string Closed = "Closed";

    /// <summary>
    /// 誤報
    /// </summary>
    public const string FalsePositive = "False Positive";

    /// <summary>
    /// 取得所有有效的狀態值
    /// </summary>
    public static readonly string[] AllStatuses = new[]
    {
        Open,
        InProgress,
        Closed,
        FalsePositive
    };

    /// <summary>
    /// 驗證狀態值是否有效
    /// </summary>
    public static bool IsValid(string status)
    {
        return AllStatuses.Contains(status);
    }
}

