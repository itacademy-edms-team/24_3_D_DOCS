using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOllamaApiKeysAndClientMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_message_id",
                table: "chat_messages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_ollama_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    encrypted_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_ollama_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_ollama_api_keys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatSessionId_ClientMessageId",
                table: "chat_messages",
                columns: new[] { "chat_session_id", "client_message_id" },
                unique: true,
                filter: "\"client_message_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserOllamaApiKeys_UserId",
                table: "user_ollama_api_keys",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_ollama_api_keys");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ChatSessionId_ClientMessageId",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "client_message_id",
                table: "chat_messages");
        }
    }
}
