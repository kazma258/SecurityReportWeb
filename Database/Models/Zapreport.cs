using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SecurityReportWeb.Database.Models;

[Table("ZAPReport")]
[Index("SiteUrlId", "GeneratedDay", Name = "UQ_ZAPReport_SiteUrlId_GeneratedDay", IsUnique = true)]
[Index("GeneratedDate", Name = "ZAPReport_index_2")]
public partial class Zapreport
{
    [Key]
    public Guid ReportId { get; set; }

    public Guid SiteUrlId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime GeneratedDate { get; set; }

    public DateOnly GeneratedDay { get; set; }

    [Column("ZAPVersion")]
    [StringLength(255)]
    public string Zapversion { get; set; } = null!;

    [Column("ZAPSupporter")]
    [StringLength(255)]
    public string Zapsupporter { get; set; } = null!;

    public bool IsDeleted { get; set; }
}
