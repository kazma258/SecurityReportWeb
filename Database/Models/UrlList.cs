using System;
using System.Collections.Generic;

namespace SecurityReportWeb.Database.Models;

public partial class UrlList
{
    public Guid UrlId { get; set; }

    public string Url { get; set; } = null!;

    public string? Ip { get; set; }

    public string WebName { get; set; } = null!;

    public string UnitName { get; set; } = null!;

    public string? Remark { get; set; }

    public string? Manager { get; set; }

    public string? ManagerMail { get; set; }

    public string? OutsourcedVendor { get; set; }

    public string RiskReportLink { get; set; } = null!;

    public DateOnly UploadDate { get; set; }

    public virtual ICollection<ZapalertDetail> ZapalertDetails { get; set; } = new List<ZapalertDetail>();

    public virtual ICollection<Zapreport> Zapreports { get; set; } = new List<Zapreport>();
}
