using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SecurityReportWeb.Database.Models;

public partial class ReportDbContext : DbContext
{
    public ReportDbContext(DbContextOptions<ReportDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RiskDescription> RiskDescriptions { get; set; }

    public virtual DbSet<UrlList> UrlLists { get; set; }

    public virtual DbSet<ZapalertDetail> ZapalertDetails { get; set; }

    public virtual DbSet<Zapreport> Zapreports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RiskDescription>(entity =>
        {
            entity.HasKey(e => e.RiskId).HasName("PK__RiskDesc__435363F6A0A1D0DB");

            entity.Property(e => e.RiskId).ValueGeneratedNever();
        });

        modelBuilder.Entity<UrlList>(entity =>
        {
            entity.HasKey(e => e.UrlId).HasName("PK__UrlLists__BDE12DF0431CD460");

            entity.Property(e => e.UrlId).ValueGeneratedNever();
        });

        modelBuilder.Entity<ZapalertDetail>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__ZAPAlert__EBB16AEDB761AB67");

            entity.Property(e => e.Status).HasDefaultValue("Open");
        });

        modelBuilder.Entity<Zapreport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__ZAPRepor__D5BD48056EA5F36E");

            entity.Property(e => e.ReportId).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
