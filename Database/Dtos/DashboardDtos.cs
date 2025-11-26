using System;
using System.Collections.Generic;

namespace SecurityReportWeb.Database.Dtos;

/// <summary>
/// 儀表板總覽統計資料傳輸物件
/// </summary>
public class DashboardOverviewDto
{
    public int UnresolvedCount { get; set; }
    public int ResolvedCount { get; set; }
    public double OverallFixRate { get; set; }
    public int HighRiskCount { get; set; }
}

/// <summary>
/// 風險等級分布資料傳輸物件
/// </summary>
public class RiskLevelDistributionDto
{
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
    public int Informational { get; set; }
}

/// <summary>
/// 掃描結果比較資料傳輸物件
/// </summary>
public class ScanComparisonDto
{
    public string Date { get; set; } = string.Empty;
    public int NewCount { get; set; }
    public int ResolvedCount { get; set; }
}

/// <summary>
/// 部門資安績效資料傳輸物件
/// </summary>
public class DepartmentPerformanceDto
{
    public string UnitName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ResolvedCount { get; set; }
    public double FixRate { get; set; }
    public string? Manager { get; set; }
}

/// <summary>
/// 更新警報狀態請求資料傳輸物件
/// </summary>
public class UpdateAlertStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

/// <summary>
/// 更新警報狀態回應資料傳輸物件
/// </summary>
public class UpdateAlertStatusResponseDto
{
    public int AlertId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// 警報狀態歷史記錄資料傳輸物件
/// </summary>
public class AlertStatusHistoryDto
{
    public int HistoryId { get; set; }
    public int AlertId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public string UpdatedByRole { get; set; } = string.Empty;
}

/// <summary>
/// 修復紀錄資料傳輸物件（用於主管查看）
/// </summary>
public class FixHistoryDto
{
    public int AlertId { get; set; }
    public string WebName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string RiskName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

