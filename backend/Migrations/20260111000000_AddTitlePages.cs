using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddTitlePages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create title_pages table
            migrationBuilder.CreateTable(
                name: "title_pages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_title_pages", x => x.id);
                    table.ForeignKey(
                        name: "FK_title_pages_users_creator_id",
                        column: x => x.creator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TitlePages_CreatorId",
                table: "title_pages",
                column: "creator_id");

            // Add title_page_id to documents table
            migrationBuilder.AddColumn<Guid>(
                name: "title_page_id",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TitlePageId",
                table: "documents",
                column: "title_page_id");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_title_pages_title_page_id",
                table: "documents",
                column: "title_page_id",
                principalTable: "title_pages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_title_pages_title_page_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TitlePageId",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "title_page_id",
                table: "documents");

            migrationBuilder.DropTable(
                name: "title_pages");
        }
    }
}
