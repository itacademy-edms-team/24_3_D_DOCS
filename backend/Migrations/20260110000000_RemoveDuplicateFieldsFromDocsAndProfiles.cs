using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RusalProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateFieldsFromDocsAndProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop index on profile_id (no longer exists)
            migrationBuilder.DropIndex(
                name: "IX_Documents_ProfileId",
                table: "documents");

            // Remove columns from documents table
            migrationBuilder.DropColumn(
                name: "name",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "profile_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "documents");

            // Remove columns from profiles table
            migrationBuilder.DropColumn(
                name: "name",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore columns in documents table
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "documents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "profile_id",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "documents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // Restore columns in profiles table
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "profiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // Restore index
            migrationBuilder.CreateIndex(
                name: "IX_Documents_ProfileId",
                table: "documents",
                column: "profile_id");
        }
    }
}
