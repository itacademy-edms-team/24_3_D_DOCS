using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmModelsAndUserOllamaModelPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "llm_models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_name = table.Column<string>(type: "text", nullable: false),
                    has_view = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_llm_models", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_ollama_model_preferences",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_model_name = table.Column<string>(type: "text", nullable: true),
                    attachment_text_model_name = table.Column<string>(type: "text", nullable: true),
                    vision_model_name = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_ollama_model_preferences", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_ollama_model_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LlmModels_ModelName",
                table: "llm_models",
                column: "model_name",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO llm_models (id, model_name, has_view) VALUES
                (gen_random_uuid(), 'minimax-m2.7:cloud', false),
                (gen_random_uuid(), 'qwen3.5:cloud', true),
                (gen_random_uuid(), 'qwen3-vl:235b-cloud', true),
                (gen_random_uuid(), 'nemotron-3-super:cloud', false),
                (gen_random_uuid(), 'qwen3-next:80b-cloud', false),
                (gen_random_uuid(), 'minimax-m2.5:cloud', false),
                (gen_random_uuid(), 'glm-5:cloud', false),
                (gen_random_uuid(), 'kimi-k2.5:cloud', false),
                (gen_random_uuid(), 'devstral-2:123b-cloud', false),
                (gen_random_uuid(), 'gemini-3-flash-preview:cloud', true),
                (gen_random_uuid(), 'glm-4.7:cloud', false),
                (gen_random_uuid(), 'minimax-m2:cloud', false),
                (gen_random_uuid(), 'deepseek-v3.2:cloud', false),
                (gen_random_uuid(), 'glm-4.6:cloud', false),
                (gen_random_uuid(), 'mistral-large-3:675b-cloud', false),
                (gen_random_uuid(), 'gpt-oss:120b-cloud', false),
                (gen_random_uuid(), 'deepseek-v3.1:671b-cloud', false),
                (gen_random_uuid(), 'kimi-k2:1t-cloud', false),
                (gen_random_uuid(), 'ministral-3:14b-cloud', true),
                (gen_random_uuid(), 'gemma3:27b-cloud', true);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM llm_models;");

            migrationBuilder.DropTable(
                name: "llm_models");

            migrationBuilder.DropTable(
                name: "user_ollama_model_preferences");
        }
    }
}
