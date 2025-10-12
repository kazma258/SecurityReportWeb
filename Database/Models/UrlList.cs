using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SecurityReportWeb.Database.Models;

[Index("WebName", Name = "UQ__UrlLists__987EC824CB54D070", IsUnique = true)]
[Index("Manager", Name = "UrlLists_index_0")]
public partial class UrlList
{
    [Key]
    public Guid UrlId { get; set; }

    [StringLength(2048)]
    public string Url { get; set; } = null!;

    [Column("IP")]
    [StringLength(255)]
    public string? Ip { get; set; }

    [StringLength(255)]
    public string WebName { get; set; } = null!;

    [StringLength(255)]
    public string UnitName { get; set; } = null!;

    public string? Remark { get; set; }

    [StringLength(255)]
    public string? Manager { get; set; }

    [StringLength(2048)]
    public string? ManagerMail { get; set; }

    [StringLength(255)]
    public string? OutsourcedVendor { get; set; }

    [StringLength(2048)]
    public string RiskReportLink { get; set; } = null!;

    public DateOnly UploadDate { get; set; }
}
