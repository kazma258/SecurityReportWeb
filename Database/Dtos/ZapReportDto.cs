using System;

namespace SecurityReportWeb.Database.Dtos;

/// <summary>
/// ZAP 報告資料傳輸物件
/// </summary>
public class ZapReportDto
{
    public Guid ReportId { get; set; }
    public Guid SiteUrlId { get; set; }
    public string? SiteWebName { get; set; }
    public string? SiteUrl { get; set; }
    public DateTime GeneratedDate { get; set; }
    public DateOnly GeneratedDay { get; set; }
    public string Zapversion { get; set; } = string.Empty;
    public string Zapsupporter { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    
    // 統計資訊（可選）
    public int? AlertCount { get; set; }
}

