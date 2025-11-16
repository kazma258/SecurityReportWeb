using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityReportWeb.Migrations
{
    /// <inheritdoc />
    public partial class RecreateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PrimaryKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "RiskDescription",
                columns: table => new
                {
                    RiskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Solution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CWEId = table.Column<int>(type: "int", nullable: true),
                    WASCId = table.Column<int>(type: "int", nullable: true),
                    PluginID = table.Column<int>(type: "int", nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RiskDesc__435363F6A0A1D0DB", x => x.RiskId);
                });

            migrationBuilder.CreateTable(
                name: "UrlLists",
                columns: table => new
                {
                    UrlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IP = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    WebName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manager = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ManagerMail = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    OutsourcedVendor = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RiskReportLink = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    UploadDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UrlLists__BDE12DF0431CD460", x => x.UrlId);
                });

            migrationBuilder.CreateTable(
                name: "ZAPReport",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteUrlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    GeneratedDay = table.Column<DateOnly>(type: "date", nullable: false),
                    ZAPVersion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ZAPSupporter = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ZAPRepor__D5BD48056EA5F36E", x => x.ReportId);
                    table.UniqueConstraint("AK_ZAPReport_SiteUrlId_GeneratedDay", x => new { x.SiteUrlId, x.GeneratedDay });
                    table.ForeignKey(
                        name: "FK_ZAPReport_UrlLists",
                        column: x => x.SiteUrlId,
                        principalTable: "UrlLists",
                        principalColumn: "UrlId");
                });

            migrationBuilder.CreateTable(
                name: "ZAPAlertDetail",
                columns: table => new
                {
                    AlertID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RootUrlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ReportDay = table.Column<DateOnly>(type: "date", nullable: false),
                    RiskName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attack = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, defaultValue: "Open"),
                    OtherInfo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ZAPAlert__EBB16AEDB761AB67", x => x.AlertID);
                    table.ForeignKey(
                        name: "FK_ZAPAlertDetail_UrlLists",
                        column: x => x.RootUrlId,
                        principalTable: "UrlLists",
                        principalColumn: "UrlId");
                    table.ForeignKey(
                        name: "FK_ZAPAlertDetail_ZAPReport",
                        columns: x => new { x.RootUrlId, x.ReportDay },
                        principalTable: "ZAPReport",
                        principalColumns: new[] { "SiteUrlId", "GeneratedDay" });
                });

            migrationBuilder.CreateIndex(
                name: "RiskDescription_index_8",
                table: "RiskDescription",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "UQ_RiskDescription_Name_Signature",
                table: "RiskDescription",
                columns: new[] { "Name", "Signature" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__UrlLists__987EC824CB54D070",
                table: "UrlLists",
                column: "WebName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UrlLists_index_0",
                table: "UrlLists",
                column: "Manager");

            migrationBuilder.CreateIndex(
                name: "ZAPAlertDetail_index_3",
                table: "ZAPAlertDetail",
                columns: new[] { "RootUrlId", "ReportDay" });

            migrationBuilder.CreateIndex(
                name: "ZAPAlertDetail_index_4",
                table: "ZAPAlertDetail",
                columns: new[] { "RootUrlId", "RiskName", "ReportDay" });

            migrationBuilder.CreateIndex(
                name: "ZAPAlertDetail_index_5",
                table: "ZAPAlertDetail",
                columns: new[] { "Url", "ReportDay" });

            migrationBuilder.CreateIndex(
                name: "ZAPAlertDetail_index_6",
                table: "ZAPAlertDetail",
                column: "RiskName");

            migrationBuilder.CreateIndex(
                name: "UQ_ZAPReport_SiteUrlId_GeneratedDay",
                table: "ZAPReport",
                columns: new[] { "SiteUrlId", "GeneratedDay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ZAPReport_index_2",
                table: "ZAPReport",
                column: "GeneratedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "RiskDescription");

            migrationBuilder.DropTable(
                name: "ZAPAlertDetail");

            migrationBuilder.DropTable(
                name: "ZAPReport");

            migrationBuilder.DropTable(
                name: "UrlLists");
        }
    }
}
