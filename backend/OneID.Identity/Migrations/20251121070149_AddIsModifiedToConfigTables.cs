using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneID.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddIsModifiedToConfigTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsModified",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsModified",
                table: "RateLimitSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsModified",
                table: "ExternalAuthProvider",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsModified",
                table: "EmailTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsModified",
                table: "EmailConfigurations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsModified",
                table: "CorsSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsModified",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "IsModified",
                table: "RateLimitSettings");

            migrationBuilder.DropColumn(
                name: "IsModified",
                table: "ExternalAuthProvider");

            migrationBuilder.DropColumn(
                name: "IsModified",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "IsModified",
                table: "EmailConfigurations");

            migrationBuilder.DropColumn(
                name: "IsModified",
                table: "CorsSettings");
        }
    }
}
