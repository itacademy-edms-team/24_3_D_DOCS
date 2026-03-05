using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RusalProject.Provider.Database;

namespace RusalProject.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260325010000_AddDocumentAiChanges")]
public partial class AddDocumentAiChanges : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS document_ai_changes (
                id uuid NOT NULL,
                document_id uuid NOT NULL,
                change_id character varying(64) NOT NULL,
                change_type character varying(16) NOT NULL,
                entity_type character varying(64) NOT NULL,
                start_line integer NOT NULL,
                end_line integer NULL,
                content text NOT NULL,
                group_id character varying(64) NULL,
                change_order integer NULL,
                created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                CONSTRAINT "PK_document_ai_changes" PRIMARY KEY (id),
                CONSTRAINT "FK_document_ai_changes_document_links_document_id"
                    FOREIGN KEY (document_id) REFERENCES document_links (id) ON DELETE CASCADE
            );
            """
        );

        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS "IX_document_ai_changes_document_id"
            ON document_ai_changes (document_id);
            """
        );

        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS "IX_document_ai_changes_change_id"
            ON document_ai_changes (change_id);
            """
        );

        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS "IX_document_ai_changes_group_id"
            ON document_ai_changes (group_id);
            """
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP TABLE IF EXISTS document_ai_changes;""");
    }
}
