using System;

namespace SecurityReportWeb.Database.Dtos;

/// <summary>
/// ZAP 警報詳情資料傳輸物件
/// </summary>
public class ZapAlertDetailDto
{
    public int AlertId { get; set; }
    public Guid RootUrlId { get; set; }
    public string? RootWebName { get; set; }
    public string? RootUrl { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public DateOnly ReportDay { get; set; }
    public string RiskName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? Parameter { get; set; }
    public string? Attack { get; set; }
    public string? Evidence { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OtherInfo { get; set; }
}

