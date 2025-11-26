using System;

namespace SecurityReportWeb.Database.Dtos;

/// <summary>
/// 風險描述資料傳輸物件
/// </summary>
public class RiskDescriptionDto
{
    public Guid RiskId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Solution { get; set; }
    public string? Reference { get; set; }
    public int? Cweid { get; set; }
    public int? Wascid { get; set; }
    public int? PluginId { get; set; }
    public string Signature { get; set; } = string.Empty;
}

