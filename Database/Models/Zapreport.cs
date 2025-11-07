using System;
using System.Collections.Generic;

namespace SecurityReportWeb.Database.Models;

public partial class Zapreport
{
    public Guid ReportId { get; set; }

    public Guid SiteUrlId { get; set; }

    public DateTime GeneratedDate { get; set; }

    public DateOnly GeneratedDay { get; set; }

    public string Zapversion { get; set; } = null!;

    public string Zapsupporter { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual UrlList SiteUrl { get; set; } = null!;

    public virtual ICollection<ZapalertDetail> ZapalertDetails { get; set; } = new List<ZapalertDetail>();
}
