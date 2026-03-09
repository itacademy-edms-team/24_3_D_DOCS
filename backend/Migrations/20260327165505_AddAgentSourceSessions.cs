using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentSourceSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_source_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ingest_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_source_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_source_sessions_chat_sessions_chat_session_id",
                        column: x => x.chat_session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_source_sessions_document_links_document_id",
                        column: x => x.document_id,
                        principalTable: "document_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_source_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_source_parts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_index = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    inline_text = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_source_parts", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_source_parts_agent_source_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "agent_source_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentSourceParts_Session_PartIndex",
                table: "agent_source_parts",
                columns: new[] { "session_id", "part_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentSourceParts_SessionId",
                table: "agent_source_parts",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSourceSessions_ChatSessionId",
                table: "agent_source_sessions",
                column: "chat_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSourceSessions_DocumentId",
                table: "agent_source_sessions",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSourceSessions_ExpiresAt",
                table: "agent_source_sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSourceSessions_UserId",
                table: "agent_source_sessions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_source_parts");

            migrationBuilder.DropTable(
                name: "agent_source_sessions");
        }
    }
}
