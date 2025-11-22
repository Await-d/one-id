using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneID.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForConfigurationPolling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 为配置表的 UpdatedAt 列添加索引，优化轮询服务性能
            migrationBuilder.CreateIndex(
                name: "IX_RateLimitSettings_UpdatedAt",
                table: "RateLimitSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CorsSettings_UpdatedAt",
                table: "CorsSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthProvider_UpdatedAt",
                table: "ExternalAuthProvider",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedAt",
                table: "SystemSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigurations_UpdatedAt",
                table: "EmailConfigurations",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_UpdatedAt",
                table: "EmailTemplates",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RateLimitSettings_UpdatedAt",
                table: "RateLimitSettings");

            migrationBuilder.DropIndex(
                name: "IX_CorsSettings_UpdatedAt",
                table: "CorsSettings");

            migrationBuilder.DropIndex(
                name: "IX_ExternalAuthProvider_UpdatedAt",
                table: "ExternalAuthProvider");

            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_UpdatedAt",
                table: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_EmailConfigurations_UpdatedAt",
                table: "EmailConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_UpdatedAt",
                table: "EmailTemplates");
        }
    }
}
