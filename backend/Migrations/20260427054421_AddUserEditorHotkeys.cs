using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEditorHotkeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_editor_hotkeys",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bindings = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_editor_hotkeys", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_editor_hotkeys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_editor_hotkeys");
        }
    }
}
