using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSchemaLinkIdFromDocumentLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_document_links_schema_links_schema_link_id",
                table: "document_links");

            migrationBuilder.DropIndex(
                name: "IX_DocumentLinks_SchemaLinkId",
                table: "document_links");

            migrationBuilder.DropColumn(
                name: "schema_link_id",
                table: "document_links");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "schema_link_id",
                table: "document_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_SchemaLinkId",
                table: "document_links",
                column: "schema_link_id");

            migrationBuilder.AddForeignKey(
                name: "FK_document_links_schema_links_schema_link_id",
                table: "document_links",
                column: "schema_link_id",
                principalTable: "schema_links",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
