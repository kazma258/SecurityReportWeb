using System;
using System.Collections.Generic;

namespace SecurityReportWeb.Database.Models;

public partial class ZapalertDetail
{
    public int AlertId { get; set; }

    public Guid RootUrlId { get; set; }

    public string Url { get; set; } = null!;

    public DateTime ReportDate { get; set; }

    public DateOnly ReportDay { get; set; }

    public string RiskName { get; set; } = null!;

    public string Level { get; set; } = null!;

    public string Method { get; set; } = null!;

    public string? Parameter { get; set; }

    public string? Attack { get; set; }

    public string? Evidence { get; set; }

    public string Status { get; set; } = null!;

    public string? OtherInfo { get; set; }

    public virtual UrlList RootUrl { get; set; } = null!;

    public virtual Zapreport? Report { get; set; }
}
