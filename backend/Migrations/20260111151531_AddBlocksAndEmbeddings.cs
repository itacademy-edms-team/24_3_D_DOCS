using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddBlocksAndEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pgvector extension - skipped as it's not installed
            // migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            // Create document_blocks table
            migrationBuilder.CreateTable(
                name: "document_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    block_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_line = table.Column<int>(type: "integer", nullable: false),
                    end_line = table.Column<int>(type: "integer", nullable: false),
                    raw_text = table.Column<string>(type: "text", nullable: false),
                    normalized_text = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_blocks_document_links_document_id",
                        column: x => x.document_id,
                        principalTable: "document_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create block_embeddings table
            migrationBuilder.CreateTable(
                name: "block_embeddings",
                columns: table => new
                {
                    block_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_block_embeddings", x => x.block_id);
                    table.ForeignKey(
                        name: "FK_block_embeddings_document_blocks_block_id",
                        column: x => x.block_id,
                        principalTable: "document_blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Add embedding column as real array (temporary workaround for vector type)
            migrationBuilder.AddColumn<float[]>(
                name: "embedding",
                table: "block_embeddings",
                type: "real[]",
                nullable: false);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_DocumentBlocks_DocumentId",
                table: "document_blocks",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBlocks_ContentHash",
                table: "document_blocks",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBlocks_DeletedAt",
                table: "document_blocks",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_BlockEmbeddings_Model",
                table: "block_embeddings",
                column: "model");

            // Note: Vector index creation skipped - using real[] instead of vector type
            // Vector similarity search will be done in-memory for now
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "block_embeddings");

            migrationBuilder.DropTable(
                name: "document_blocks");

            // Note: We don't drop the vector extension as it might be used by other tables
        }
    }
}
