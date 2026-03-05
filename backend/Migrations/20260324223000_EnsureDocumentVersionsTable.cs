using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RusalProject.Provider.Database;

#nullable disable

namespace RusalProject.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324223000_EnsureDocumentVersionsTable")]
    public class EnsureDocumentVersionsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS document_versions (
                    id uuid NOT NULL,
                    document_id uuid NOT NULL,
                    name character varying(255) NOT NULL,
                    content_minio_path character varying(500) NOT NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    CONSTRAINT "PK_document_versions" PRIMARY KEY (id),
                    CONSTRAINT "FK_document_versions_document_links_document_id"
                        FOREIGN KEY (document_id) REFERENCES document_links (id) ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_DocumentVersions_DocumentId"
                ON document_versions (document_id);
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_DocumentVersions_CreatedAt"
                ON document_versions (created_at);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_DocumentVersions_CreatedAt";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_DocumentVersions_DocumentId";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS document_versions;""");
        }
    }
}
