using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnetstore.PersonalApp.Modules.Audit.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261112133350_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Operation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                IsWrite = table.Column<bool>(type: "INTEGER", nullable: false),
                EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                ErrorCode = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Fields = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                ElapsedMs = table.Column<double>(type: "REAL", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditRecords", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditRecords_EntityId",
            table: "AuditRecords",
            column: "EntityId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditRecords_UserId",
            table: "AuditRecords",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditRecords");
    }
}
