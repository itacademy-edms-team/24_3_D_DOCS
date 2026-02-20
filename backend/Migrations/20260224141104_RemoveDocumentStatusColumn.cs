using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentLinks_Status",
                table: "document_links");

            migrationBuilder.DropColumn(
                name: "status",
                table: "document_links");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "document_links",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "draft");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_Status",
                table: "document_links",
                column: "status");
        }
    }
}
