using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class FixDocumentProfileFkToProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_document_links_schema_links_profile_id",
                table: "document_links");

            migrationBuilder.AddForeignKey(
                name: "FK_document_links_profiles_profile_id",
                table: "document_links",
                column: "profile_id",
                principalTable: "profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_document_links_profiles_profile_id",
                table: "document_links");

            migrationBuilder.AddForeignKey(
                name: "FK_document_links_schema_links_profile_id",
                table: "document_links",
                column: "profile_id",
                principalTable: "schema_links",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
