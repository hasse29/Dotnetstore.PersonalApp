using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnetstore.PersonalApp.Modules.Email.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftsCcHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cc",
                table: "SentMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CcAddresses",
                table: "ReceivedMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HtmlBody",
                table: "ReceivedMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToAddresses",
                table: "ReceivedMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DraftId",
                table: "MailAttachments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    To = table.Column<string>(type: "TEXT", nullable: false),
                    Cc = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    IsHtml = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drafts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailAttachments_DraftId",
                table: "MailAttachments",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_UserId_UpdatedAt",
                table: "Drafts",
                columns: new[] { "UserId", "UpdatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_MailAttachments_Drafts_DraftId",
                table: "MailAttachments",
                column: "DraftId",
                principalTable: "Drafts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MailAttachments_Drafts_DraftId",
                table: "MailAttachments");

            migrationBuilder.DropTable(
                name: "Drafts");

            migrationBuilder.DropIndex(
                name: "IX_MailAttachments_DraftId",
                table: "MailAttachments");

            migrationBuilder.DropColumn(
                name: "Cc",
                table: "SentMessages");

            migrationBuilder.DropColumn(
                name: "CcAddresses",
                table: "ReceivedMessages");

            migrationBuilder.DropColumn(
                name: "HtmlBody",
                table: "ReceivedMessages");

            migrationBuilder.DropColumn(
                name: "ToAddresses",
                table: "ReceivedMessages");

            migrationBuilder.DropColumn(
                name: "DraftId",
                table: "MailAttachments");
        }
    }
}
