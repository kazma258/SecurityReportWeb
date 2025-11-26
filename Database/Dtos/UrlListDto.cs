using System;

namespace SecurityReportWeb.Database.Dtos;

/// <summary>
/// URL 清單資料傳輸物件
/// </summary>
public class UrlListDto
{
    public Guid UrlId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public string WebName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? Manager { get; set; }
    public string? ManagerMail { get; set; }
    public string? OutsourcedVendor { get; set; }
    public string RiskReportLink { get; set; } = string.Empty;
    public DateOnly UploadDate { get; set; }
    
    // 統計資訊（可選）
    public int? ReportCount { get; set; }
    public int? AlertCount { get; set; }
}

