using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityReportWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ZAPAlertDetail_ReportDay_Status",
                table: "ZAPAlertDetail",
                columns: new[] { "ReportDay", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ZAPAlertDetail_Status_Level",
                table: "ZAPAlertDetail",
                columns: new[] { "Status", "Level" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ZAPAlertDetail_ReportDay_Status",
                table: "ZAPAlertDetail");

            migrationBuilder.DropIndex(
                name: "IX_ZAPAlertDetail_Status_Level",
                table: "ZAPAlertDetail");
        }
    }
}
