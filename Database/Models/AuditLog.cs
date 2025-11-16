using System;

namespace SecurityReportWeb.Database.Models;

public partial class AuditLog
{
    public long AuditId { get; set; }

    public string TableName { get; set; } = null!;

    public string PrimaryKey { get; set; } = null!;

    public string Operation { get; set; } = null!; // Added, Modified, Deleted

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public DateTime ChangedAtUtc { get; set; }

    public string? ChangedBy { get; set; }
}
