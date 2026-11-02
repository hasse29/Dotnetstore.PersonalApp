using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnetstore.PersonalApp.Modules.Schedule.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleEntryStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleEntryStudents",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEntryStudents", x => new { x.EntryId, x.StudentId });
                    table.ForeignKey(
                        name: "FK_ScheduleEntryStudents_ScheduleEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "ScheduleEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntryStudents_StudentId",
                table: "ScheduleEntryStudents",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleEntryStudents");
        }
    }
}
