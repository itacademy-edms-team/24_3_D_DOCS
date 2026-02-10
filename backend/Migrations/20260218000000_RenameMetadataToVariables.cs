using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RusalProject.Provider.Database;

#nullable disable

namespace RusalProject.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260218000000_RenameMetadataToVariables")]
    public partial class RenameMetadataToVariables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add variables column
            migrationBuilder.AddColumn<string>(
                name: "variables",
                table: "document_links",
                type: "jsonb",
                nullable: true);

            // Migrate data: convert metadata (camelCase) to variables (PascalCase + additionalFields)
            migrationBuilder.Sql(@"
                UPDATE document_links
                SET variables = (
                    jsonb_build_object(
                        'Title', COALESCE(metadata->>'title', ''),
                        'Author', COALESCE(metadata->>'author', ''),
                        'Group', COALESCE(metadata->>'group', ''),
                        'Year', COALESCE(metadata->>'year', ''),
                        'City', COALESCE(metadata->>'city', ''),
                        'Supervisor', COALESCE(metadata->>'supervisor', ''),
                        'DocumentType', COALESCE(metadata->>'documentType', '')
                    ) || COALESCE(metadata->'additionalFields', '{}'::jsonb)
                )
                WHERE metadata IS NOT NULL;
            ");

            migrationBuilder.DropColumn(
                name: "metadata",
                table: "document_links");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "metadata",
                table: "document_links",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE document_links
                SET metadata = jsonb_build_object(
                    'title', COALESCE(variables->>'Title', ''),
                    'author', COALESCE(variables->>'Author', ''),
                    'group', COALESCE(variables->>'Group', ''),
                    'year', COALESCE(variables->>'Year', ''),
                    'city', COALESCE(variables->>'City', ''),
                    'supervisor', COALESCE(variables->>'Supervisor', ''),
                    'documentType', COALESCE(variables->>'DocumentType', '')
                )
                WHERE variables IS NOT NULL;
            ");

            migrationBuilder.DropColumn(
                name: "variables",
                table: "document_links");
        }
    }
}
