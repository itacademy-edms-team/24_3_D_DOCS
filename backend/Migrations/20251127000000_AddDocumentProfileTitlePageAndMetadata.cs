using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentProfileTitlePageAndMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Создаём таблицу title_pages
            migrationBuilder.CreateTable(
                name: "title_pages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    minio_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
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

            // Добавляем новые поля в document_links
            migrationBuilder.AddColumn<Guid>(
                name: "profile_id",
                table: "document_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "title_page_id",
                table: "document_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata",
                table: "document_links",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                table: "document_links",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "document_links",
                type: "timestamp with time zone",
                nullable: true);

            // Создаём индексы
            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_ProfileId",
                table: "document_links",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_TitlePageId",
                table: "document_links",
                column: "title_page_id");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_IsArchived",
                table: "document_links",
                column: "is_archived");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_DeletedAt",
                table: "document_links",
                column: "deleted_at");

            // Добавляем внешние ключи
            migrationBuilder.AddForeignKey(
                name: "FK_document_links_schema_links_profile_id",
                table: "document_links",
                column: "profile_id",
                principalTable: "schema_links",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_document_links_title_pages_title_page_id",
                table: "document_links",
                column: "title_page_id",
                principalTable: "title_pages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удаляем внешние ключи
            migrationBuilder.DropForeignKey(
                name: "FK_document_links_schema_links_profile_id",
                table: "document_links");

            migrationBuilder.DropForeignKey(
                name: "FK_document_links_title_pages_title_page_id",
                table: "document_links");

            // Удаляем индексы
            migrationBuilder.DropIndex(
                name: "IX_DocumentLinks_DeletedAt",
                table: "document_links");

            migrationBuilder.DropIndex(
                name: "IX_DocumentLinks_IsArchived",
                table: "document_links");

            migrationBuilder.DropIndex(
                name: "IX_DocumentLinks_TitlePageId",
                table: "document_links");

            migrationBuilder.DropIndex(
                name: "IX_DocumentLinks_ProfileId",
                table: "document_links");

            // Удаляем колонки из document_links
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "document_links");

            migrationBuilder.DropColumn(
                name: "is_archived",
                table: "document_links");

            migrationBuilder.DropColumn(
                name: "metadata",
                table: "document_links");

            migrationBuilder.DropColumn(
                name: "title_page_id",
                table: "document_links");

            migrationBuilder.DropColumn(
                name: "profile_id",
                table: "document_links");

            // Удаляем таблицу title_pages
            migrationBuilder.DropTable(
                name: "title_pages");
        }
    }
}
