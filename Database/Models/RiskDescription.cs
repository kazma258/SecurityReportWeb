using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SecurityReportWeb.Database.Models;

[Table("RiskDescription")]
[Index("Name", Name = "RiskDescription_index_8")]
[Index("Name", "Signature", Name = "UQ_RiskDescription_Name_Signature", IsUnique = true)]
public partial class RiskDescription
{
    [Key]
    public Guid RiskId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Solution { get; set; }

    [StringLength(2048)]
    public string? Reference { get; set; }

    [Column("CWEId")]
    public int? Cweid { get; set; }

    [Column("WASCId")]
    public int? Wascid { get; set; }

    [Column("PluginID")]
    public int? PluginId { get; set; }

    [StringLength(255)]
    required public string Signature { get; set; }
}
