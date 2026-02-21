using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClientMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ChatSessionId_ClientMessageId",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "client_message_id",
                table: "chat_messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_message_id",
                table: "chat_messages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatSessionId_ClientMessageId",
                table: "chat_messages",
                columns: new[] { "chat_session_id", "client_message_id" },
                unique: true,
                filter: "\"client_message_id\" IS NOT NULL");
        }
    }
}
