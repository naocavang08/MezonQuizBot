using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuditLogDetailsDto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Details",
                table: "AuditLogs",
                newName: "details");

            migrationBuilder.AlterColumn<string>(
                name: "details",
                table: "AuditLogs",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(JsonDocument),
                oldType: "jsonb",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "details",
                table: "AuditLogs",
                newName: "Details");

            migrationBuilder.AlterColumn<JsonDocument>(
                name: "Details",
                table: "AuditLogs",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");
        }
    }
}
