using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RusalProject.Provider.Database;

#nullable disable

namespace RusalProject.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260327120000_RemoveSoftDeletes")]
    public class RemoveSoftDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_document_links_document_id",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_DeletedAt",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_DeletedAt",
                table: "chat_sessions");

            migrationBuilder.DropIndex(
                name: "IX_DocumentLinks_DeletedAt",
                table: "document_links");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "document_links");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_document_links_document_id",
                table: "attachments",
                column: "document_id",
                principalTable: "document_links",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_document_links_document_id",
                table: "attachments");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "document_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "chat_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "attachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_DeletedAt",
                table: "document_links",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_DeletedAt",
                table: "chat_sessions",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_DeletedAt",
                table: "attachments",
                column: "deleted_at");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_document_links_document_id",
                table: "attachments",
                column: "document_id",
                principalTable: "document_links",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
