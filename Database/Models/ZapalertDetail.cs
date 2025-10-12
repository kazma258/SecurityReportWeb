using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SecurityReportWeb.Database.Models;

[Table("ZAPAlertDetail")]
[Index("RootUrlId", "ReportDay", Name = "ZAPAlertDetail_index_3")]
[Index("RootUrlId", "RiskName", "ReportDay", Name = "ZAPAlertDetail_index_4")]
[Index("Url", "ReportDay", Name = "ZAPAlertDetail_index_5")]
[Index("RiskName", Name = "ZAPAlertDetail_index_6")]
public partial class ZapalertDetail
{
    [Key]
    [Column("AlertID")]
    public int AlertId { get; set; }

    public Guid RootUrlId { get; set; }

    [StringLength(2048)]
    public string Url { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime ReportDate { get; set; }

    public DateOnly ReportDay { get; set; }

    [StringLength(255)]
    public string RiskName { get; set; } = null!;

    [StringLength(255)]
    public string Level { get; set; } = null!;

    [StringLength(255)]
    public string Method { get; set; } = null!;

    public string? Parameter { get; set; }

    public string? Attack { get; set; }

    public string? Evidence { get; set; }

    [StringLength(255)]
    public string Status { get; set; } = null!;

    public string? OtherInfo { get; set; }
}
