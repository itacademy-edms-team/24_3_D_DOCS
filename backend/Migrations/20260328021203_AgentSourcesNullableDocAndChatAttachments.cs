using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class AgentSourcesNullableDocAndChatAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agent_source_sessions_document_links_document_id",
                table: "agent_source_sessions");

            migrationBuilder.AddColumn<string>(
                name: "attachments_json",
                table: "chat_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "document_id",
                table: "agent_source_sessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "original_content_type",
                table: "agent_source_sessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "original_size",
                table: "agent_source_sessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_storage_path",
                table: "agent_source_sessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_source_sessions_document_links_document_id",
                table: "agent_source_sessions",
                column: "document_id",
                principalTable: "document_links",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agent_source_sessions_document_links_document_id",
                table: "agent_source_sessions");

            migrationBuilder.DropColumn(
                name: "attachments_json",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "original_content_type",
                table: "agent_source_sessions");

            migrationBuilder.DropColumn(
                name: "original_size",
                table: "agent_source_sessions");

            migrationBuilder.DropColumn(
                name: "original_storage_path",
                table: "agent_source_sessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "document_id",
                table: "agent_source_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_source_sessions_document_links_document_id",
                table: "agent_source_sessions",
                column: "document_id",
                principalTable: "document_links",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
