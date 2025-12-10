using System;
using System.ComponentModel.DataAnnotations;

namespace SecurityReportWeb.Database.Dtos;

/// <summary>
/// 儀表板總覽統計回應 DTO
/// </summary>
public class DashboardOverviewDto
{
    /// <summary>
    /// 尚未解決的漏洞數量（Status != "Closed"）
    /// </summary>
    public int UnresolvedCount { get; set; }

    /// <summary>
    /// 已修復的漏洞數量（Status == "Closed"）
    /// </summary>
    public int ResolvedCount { get; set; }

    /// <summary>
    /// 整體修復率（百分比，0-100）
    /// 計算公式：(ResolvedCount / TotalCount) × 100
    /// </summary>
    public double OverallFixRate { get; set; }

    /// <summary>
    /// 高風險漏洞數量（Level == "High" 且 Status != "Closed"）
    /// </summary>
    public int HighRiskCount { get; set; }
}

/// <summary>
/// 風險等級分布回應 DTO
/// </summary>
public class RiskLevelDistributionDto
{
    /// <summary>
    /// 高風險數量
    /// </summary>
    public int High { get; set; }

    /// <summary>
    /// 中風險數量
    /// </summary>
    public int Medium { get; set; }

    /// <summary>
    /// 低風險數量
    /// </summary>
    public int Low { get; set; }

    /// <summary>
    /// 資訊性風險數量
    /// </summary>
    public int Informational { get; set; }
}

/// <summary>
/// 掃描結果比較回應 DTO
/// </summary>
public class ScanComparisonDto
{
    /// <summary>
    /// 日期（格式：YYYY-MM-DD）
    /// </summary>
    public string Date { get; set; } = null!;

    /// <summary>
    /// 該日期新增的警報數量
    /// </summary>
    public int NewCount { get; set; }

    /// <summary>
    /// 該日期修復的警報數量
    /// </summary>
    public int ResolvedCount { get; set; }
}

/// <summary>
/// 部門資安績效回應 DTO
/// </summary>
public class DepartmentPerformanceDto
{
    /// <summary>
    /// 單位名稱
    /// </summary>
    public string UnitName { get; set; } = null!;

    /// <summary>
    /// 高風險數量
    /// </summary>
    public int HighRiskCount { get; set; }

    /// <summary>
    /// 中風險數量
    /// </summary>
    public int MediumRiskCount { get; set; }

    /// <summary>
    /// 低風險數量
    /// </summary>
    public int LowRiskCount { get; set; }

    /// <summary>
    /// 總警報數量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已修復數量
    /// </summary>
    public int ResolvedCount { get; set; }

    /// <summary>
    /// 修復率（百分比，0-100）
    /// </summary>
    public double FixRate { get; set; }

    /// <summary>
    /// 負責人
    /// </summary>
    public string? Manager { get; set; }
}

/// <summary>
/// 更新警報狀態請求 DTO
/// </summary>
public class AlertStatusUpdateRequestDto
{
    /// <summary>
    /// 新狀態值（必填）
    /// 允許值：Open, In Progress, Closed, False Positive
    /// </summary>
    [Required]
    [RegularExpression("^(Open|In Progress|Closed|False Positive)$", 
        ErrorMessage = "狀態值必須為：Open, In Progress, Closed, False Positive")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// 修復備註（選填）
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新警報狀態回應 DTO
/// </summary>
public class AlertStatusUpdateResponseDto
{
    /// <summary>
    /// 警報 ID
    /// </summary>
    public int AlertId { get; set; }

    /// <summary>
    /// 更新後的狀態
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// 更新時間（ISO 8601 格式）
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 更新者名稱
    /// </summary>
    public string UpdatedBy { get; set; } = null!;
}

/// <summary>
/// 警報狀態歷史記錄 DTO
/// </summary>
public class AlertStatusHistoryDto
{
    /// <summary>
    /// 歷史記錄 ID
    /// </summary>
    public int HistoryId { get; set; }

    /// <summary>
    /// 警報 ID
    /// </summary>
    public int AlertId { get; set; }

    /// <summary>
    /// 舊狀態
    /// </summary>
    public string? OldStatus { get; set; }

    /// <summary>
    /// 新狀態
    /// </summary>
    public string NewStatus { get; set; } = null!;

    /// <summary>
    /// 備註
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 更新時間（ISO 8601 格式）
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 更新者名稱
    /// </summary>
    public string UpdatedBy { get; set; } = null!;

    /// <summary>
    /// 更新者角色
    /// </summary>
    public string UpdatedByRole { get; set; } = null!;
}

/// <summary>
/// 修復紀錄項目 DTO
/// </summary>
public class FixHistoryItemDto
{
    /// <summary>
    /// 警報 ID
    /// </summary>
    public int AlertId { get; set; }

    /// <summary>
    /// 網站名稱
    /// </summary>
    public string WebName { get; set; } = null!;

    /// <summary>
    /// 單位名稱
    /// </summary>
    public string UnitName { get; set; } = null!;

    /// <summary>
    /// 風險名稱
    /// </summary>
    public string RiskName { get; set; } = null!;

    /// <summary>
    /// 風險等級
    /// </summary>
    public string Level { get; set; } = null!;

    /// <summary>
    /// 狀態
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// 更新時間（ISO 8601 格式）
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 更新者名稱
    /// </summary>
    public string UpdatedBy { get; set; } = null!;

    /// <summary>
    /// 備註
    /// </summary>
    public string? Remark { get; set; }
}
