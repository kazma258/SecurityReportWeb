using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SecurityReportWeb.Database.Models;

public partial class ReportDbContext : DbContext
{
    public ReportDbContext()
    {
    }

    public ReportDbContext(DbContextOptions<ReportDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RiskDescription> RiskDescriptions { get; set; }

    public virtual DbSet<UrlList> UrlLists { get; set; }

    public virtual DbSet<ZapalertDetail> ZapalertDetails { get; set; }

    public virtual DbSet<Zapreport> Zapreports { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
        }
    }

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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
