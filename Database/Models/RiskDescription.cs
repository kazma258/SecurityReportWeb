using System;
using System.Collections.Generic;

namespace SecurityReportWeb.Database.Models;

public partial class RiskDescription
{
    public Guid RiskId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Solution { get; set; }

    public string? Reference { get; set; }

    public int? Cweid { get; set; }

    public int? Wascid { get; set; }

    public int? PluginId { get; set; }

    public string Signature { get; set; } = null!;
}
