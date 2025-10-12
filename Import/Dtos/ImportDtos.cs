using System;
using System.Collections.Generic;

namespace SecurityReportWeb.Import.Dtos;

public class UrlListImportDto
{
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
}

public class RiskDescriptionImportDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Solution { get; set; }
    public string? Reference { get; set; }
    public int? Cweid { get; set; }
    public int? Wascid { get; set; }
    public int? PluginId { get; set; }
    // Signature 會由系統根據上述欄位自動計算，不需手動提供
}

public class ZapReportImportDto
{
    // 自然鍵：以 WebName 對應 UrlList
    public string SiteWebName { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public DateOnly GeneratedDay { get; set; }
    public string Zapversion { get; set; } = string.Empty;
    public string Zapsupporter { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

public class ZapAlertDetailImportDto
{
    // 自然鍵：以 WebName 對應 RootUrlId
    public string RootWebName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public DateOnly ReportDay { get; set; }
    public string RiskName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? Parameter { get; set; }
    public string? Attack { get; set; }
    public string? Evidence { get; set; }
    public string? Status { get; set; }
    public string? OtherInfo { get; set; }
}

public class ImportRequestDto
{
    public List<UrlListImportDto> UrlLists { get; set; } = new();
    public List<RiskDescriptionImportDto> RiskDescriptions { get; set; } = new();
    public List<ZapReportImportDto> ZapReports { get; set; } = new();
    public List<ZapAlertDetailImportDto> ZapAlerts { get; set; } = new();

    // 若為 true，針對提交之 RootWebName+ReportDay 先移除舊有 Alerts 再匯入（避免重複）
    public bool ReplaceAlertsForSubmittedDays { get; set; }
}

public class ImportResultDto
{
    public int UrlListsInserted { get; set; }
    public int UrlListsUpdated { get; set; }
    public int RiskDescriptionsInserted { get; set; }
    public int RiskDescriptionsUpdated { get; set; }
    public int ZapReportsInserted { get; set; }
    public int ZapReportsUpdated { get; set; }
    public int ZapReportsSkipped { get; set; }
    public int ZapAlertsInserted { get; set; }
    public int ZapAlertsSkipped { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> SkippedReasons { get; set; } = new();
}


