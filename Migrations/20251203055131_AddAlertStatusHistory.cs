using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityReportWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertStatusHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    OldStatus = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedByRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertStatusHistory_HistoryId", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_AlertStatusHistory_ZAPAlertDetail",
                        column: x => x.AlertId,
                        principalTable: "ZAPAlertDetail",
                        principalColumn: "AlertID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertStatusHistory_AlertId",
                table: "AlertStatusHistory",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertStatusHistory_NewStatus",
                table: "AlertStatusHistory",
                column: "NewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AlertStatusHistory_UpdatedAt",
                table: "AlertStatusHistory",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertStatusHistory");
        }
    }
}
