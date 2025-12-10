using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Text.Json;

namespace SecurityReportWeb.Database.Models;

public partial class ReportDbContext : DbContext
{
    /// <summary>
    /// 無參數建構子；當未透過 DI 建構時供設計時工具或遷移使用。
    /// </summary>
    public ReportDbContext()
    {
    }

    /// <summary>
    /// 以外部注入的 <see cref="DbContextOptions{ReportDbContext}"/> 建構，正常執行期由 DI 使用。
    /// </summary>
    public ReportDbContext(DbContextOptions<ReportDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RiskDescription> RiskDescriptions { get; set; }

    public virtual DbSet<UrlList> UrlLists { get; set; }

    public virtual DbSet<ZapalertDetail> ZapalertDetails { get; set; }

    public virtual DbSet<Zapreport> Zapreports { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<AlertStatusHistory> AlertStatusHistories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    private bool _isAuditing = false;

    /// <summary>
    /// 覆寫同步存檔；先擷取變更生成審計資料，再寫入業務資料，最後寫入審計紀錄。
    /// </summary>
    public override int SaveChanges()
    {
        if (_isAuditing)
        {
            return base.SaveChanges();
        }

        var pending = CaptureChanges();
        var affected = base.SaveChanges();

        if (pending.Count > 0)
        {
            _isAuditing = true;
            try
            {
                foreach (var log in pending)
                {
                    AuditLogs.Add(log);
                }
                base.SaveChanges();
            }
            finally
            {
                _isAuditing = false;
            }
        }

        return affected;
    }

    /// <summary>
    /// 覆寫非同步存檔；先擷取變更生成審計資料，再寫入業務資料，最後寫入審計紀錄。
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isAuditing)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        var pending = CaptureChanges();
        var affected = await base.SaveChangesAsync(cancellationToken);

        if (pending.Count > 0)
        {
            _isAuditing = true;
            try
            {
                foreach (var log in pending)
                {
                    AuditLogs.Add(log);
                }
                await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _isAuditing = false;
            }
        }

        return affected;
    }

    /// <summary>
    /// 掃描 ChangeTracker，將本次變更轉為審計物件集合（不包含主鍵與未變動欄位）。
    /// </summary>
    private List<AuditLog> CaptureChanges()
    {
        var logs = new List<AuditLog>();
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog)
            {
                continue; // 不記錄審計表本身
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                var key = GetPrimaryKeyString(entry);

                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.Metadata.IsPrimaryKey()) continue;
                        if (entry.State == EntityState.Modified && !prop.IsModified) continue;
                        oldValues[prop.Metadata.Name] = prop.OriginalValue;
                    }
                }

                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.Metadata.IsPrimaryKey()) continue;
                        if (entry.State == EntityState.Modified && !prop.IsModified) continue;
                        newValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                var operation = entry.State switch
                {
                    EntityState.Added => "Added",
                    EntityState.Modified => "Modified",
                    EntityState.Deleted => "Deleted",
                    _ => "Unknown"
                };

                var log = new AuditLog
                {
                    TableName = tableName,
                    PrimaryKey = key,
                    Operation = operation,
                    OldValuesJson = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
                    NewValuesJson = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
                    ChangedAtUtc = now,
                    ChangedBy = null // 可接入目前使用者（例如從 HttpContext 取得）
                };

                logs.Add(log);
            }
        }

        return logs;
    }

    /// <summary>
    /// 取得實體主鍵的字串表示（支援複合主鍵），格式：Key=Value&Key2=Value2。
    /// </summary>
    private static string GetPrimaryKeyString(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var keyProps = entry.Properties.Where(p => p.Metadata.IsPrimaryKey());
        var parts = new List<string>();
        foreach (var kp in keyProps)
        {
            var name = kp.Metadata.Name;
            var value = kp.CurrentValue ?? kp.OriginalValue;
            parts.Add($"{name}={value}");
        }
        return string.Join("&", parts);
    }

    /// <summary>
    /// 當未由 DI 設定時，提供後援連線設定（使用 DefaultConnection）。
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
        }
    }

    /// <summary>
    /// 以 Fluent API 設定資料表、索引、欄位型態，以及實體間關聯與外鍵。
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RiskDescription>(entity =>
        {
            entity.HasKey(e => e.RiskId).HasName("PK__RiskDesc__435363F6A0A1D0DB");

            entity.ToTable("RiskDescription");

            entity.HasIndex(e => e.Name, "RiskDescription_index_8");

            entity.HasIndex(e => new { e.Name, e.Signature }, "UQ_RiskDescription_Name_Signature").IsUnique();

            entity.Property(e => e.RiskId).ValueGeneratedNever();
            entity.Property(e => e.Cweid).HasColumnName("CWEId");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.PluginId).HasColumnName("PluginID");
            entity.Property(e => e.Reference).HasMaxLength(2048);
            entity.Property(e => e.Signature).HasMaxLength(255);
            entity.Property(e => e.Wascid).HasColumnName("WASCId");
        });

        modelBuilder.Entity<UrlList>(entity =>
        {
            entity.HasKey(e => e.UrlId).HasName("PK__UrlLists__BDE12DF0431CD460");

            entity.HasIndex(e => e.WebName, "UQ__UrlLists__987EC824CB54D070").IsUnique();

            entity.HasIndex(e => e.Manager, "UrlLists_index_0");

            entity.Property(e => e.UrlId).ValueGeneratedNever();
            entity.Property(e => e.Ip)
                .HasMaxLength(255)
                .HasColumnName("IP");
            entity.Property(e => e.Manager).HasMaxLength(255);
            entity.Property(e => e.ManagerMail).HasMaxLength(2048);
            entity.Property(e => e.OutsourcedVendor).HasMaxLength(255);
            entity.Property(e => e.RiskReportLink).HasMaxLength(2048);
            entity.Property(e => e.UnitName).HasMaxLength(255);
            entity.Property(e => e.Url).HasMaxLength(2048);
            entity.Property(e => e.WebName).HasMaxLength(255);
        });

        modelBuilder.Entity<ZapalertDetail>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__ZAPAlert__EBB16AEDB761AB67");

            entity.ToTable("ZAPAlertDetail");

            entity.HasIndex(e => new { e.RootUrlId, e.ReportDay }, "ZAPAlertDetail_index_3");

            entity.HasIndex(e => new { e.RootUrlId, e.RiskName, e.ReportDay }, "ZAPAlertDetail_index_4");

            entity.HasIndex(e => new { e.Url, e.ReportDay }, "ZAPAlertDetail_index_5");

            entity.HasIndex(e => e.RiskName, "ZAPAlertDetail_index_6");

            // 優化儀表板查詢：依狀態和風險等級查詢
            entity.HasIndex(e => new { e.Status, e.Level }, "IX_ZAPAlertDetail_Status_Level");

            // 優化時間範圍查詢：依報告日期和狀態查詢
            entity.HasIndex(e => new { e.ReportDay, e.Status }, "IX_ZAPAlertDetail_ReportDay_Status");

            entity.Property(e => e.AlertId).HasColumnName("AlertID");
            entity.Property(e => e.Level).HasMaxLength(255);
            entity.Property(e => e.Method).HasMaxLength(255);
            entity.Property(e => e.ReportDate).HasColumnType("datetime");
            entity.Property(e => e.RiskName).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(255)
                .HasDefaultValue("Open");
            entity.Property(e => e.Url).HasMaxLength(2048);

            entity.HasOne(d => d.RootUrl).WithMany(p => p.ZapalertDetails)
                .HasForeignKey(d => d.RootUrlId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ZAPAlertDetail_UrlLists");

            entity.HasOne(d => d.Report).WithMany(p => p.ZapalertDetails)
                .HasForeignKey(d => new { d.RootUrlId, d.ReportDay })
                .HasPrincipalKey(p => new { p.SiteUrlId, p.GeneratedDay })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ZAPAlertDetail_ZAPReport");
        });

        modelBuilder.Entity<Zapreport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__ZAPRepor__D5BD48056EA5F36E");

            entity.ToTable("ZAPReport");

            entity.HasIndex(e => new { e.SiteUrlId, e.GeneratedDay }, "UQ_ZAPReport_SiteUrlId_GeneratedDay").IsUnique();

            entity.HasIndex(e => e.GeneratedDate, "ZAPReport_index_2");

            entity.Property(e => e.ReportId).ValueGeneratedNever();
            entity.Property(e => e.GeneratedDate).HasColumnType("datetime");
            entity.Property(e => e.Zapsupporter)
                .HasMaxLength(255)
                .HasColumnName("ZAPSupporter");
            entity.Property(e => e.Zapversion)
                .HasMaxLength(255)
                .HasColumnName("ZAPVersion");

            entity.HasOne(d => d.SiteUrl).WithMany(p => p.Zapreports)
                .HasForeignKey(d => d.SiteUrlId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ZAPReport_UrlLists");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.AuditId).ValueGeneratedOnAdd();
            entity.Property(e => e.TableName).HasMaxLength(255);
            entity.Property(e => e.Operation).HasMaxLength(20);
            entity.Property(e => e.PrimaryKey).HasMaxLength(512);
            entity.Property(e => e.ChangedAtUtc).HasColumnType("datetime2");
            entity.Property(e => e.ChangedBy).HasMaxLength(255);
            entity.ToTable("AuditLogs");
        });

        modelBuilder.Entity<AlertStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK_AlertStatusHistory_HistoryId");

            entity.ToTable("AlertStatusHistory");

            entity.Property(e => e.HistoryId).ValueGeneratedOnAdd();
            entity.Property(e => e.OldStatus).HasMaxLength(255);
            entity.Property(e => e.NewStatus)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedBy)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.UpdatedByRole)
                .IsRequired()
                .HasMaxLength(50);

            // 外鍵關聯到 ZAPAlertDetail
            entity.HasOne(d => d.Alert)
                .WithMany()
                .HasForeignKey(d => d.AlertId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AlertStatusHistory_ZAPAlertDetail");

            // 索引：優化依警報 ID 查詢狀態歷史的效能
            entity.HasIndex(e => e.AlertId, "IX_AlertStatusHistory_AlertId");

            // 索引：優化依時間範圍查詢的效能
            entity.HasIndex(e => e.UpdatedAt, "IX_AlertStatusHistory_UpdatedAt");

            // 索引：優化依狀態查詢的效能
            entity.HasIndex(e => e.NewStatus, "IX_AlertStatusHistory_NewStatus");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_User_UserId");

            entity.ToTable("Users");

            // 使用者帳號唯一索引
            entity.HasIndex(e => e.Username, "UQ_User_Username").IsUnique();

            // 電子郵件唯一索引（如果提供）
            entity.HasIndex(e => e.Email, "IX_User_Email")
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL");

            // 優化登入查詢：依帳號和啟用狀態查詢
            entity.HasIndex(e => new { e.Username, e.IsActive }, "IX_User_Username_IsActive");

            entity.Property(e => e.UserId).ValueGeneratedOnAdd();
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LastLoginAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK_Role_RoleId");

            entity.ToTable("Roles");

            // 角色名稱唯一索引
            entity.HasIndex(e => e.RoleName, "UQ_Role_RoleName").IsUnique();

            entity.Property(e => e.RoleId).ValueGeneratedOnAdd();
            entity.Property(e => e.RoleName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.UserRoleId).HasName("PK_UserRole_UserRoleId");

            entity.ToTable("UserRoles");

            // 防止重複的使用者角色關聯
            entity.HasIndex(e => new { e.UserId, e.RoleId }, "UQ_UserRole_UserId_RoleId").IsUnique();

            // 優化查詢：依使用者 ID 查詢角色
            entity.HasIndex(e => e.UserId, "IX_UserRole_UserId");

            // 優化查詢：依角色 ID 查詢使用者
            entity.HasIndex(e => e.RoleId, "IX_UserRole_RoleId");

            entity.Property(e => e.UserRoleId).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.RoleId).IsRequired();

            // 外鍵關聯到 User
            entity.HasOne(d => d.User)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserRole_User");

            // 外鍵關聯到 Role
            entity.HasOne(d => d.Role)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserRole_Role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
