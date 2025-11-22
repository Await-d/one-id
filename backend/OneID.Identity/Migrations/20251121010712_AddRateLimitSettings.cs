using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OneID.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddRateLimitSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RateLimitSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LimiterName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    PermitLimit = table.Column<int>(type: "integer", nullable: false),
                    WindowSeconds = table.Column<int>(type: "integer", nullable: false),
                    QueueLimit = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitSettings_Enabled",
                table: "RateLimitSettings",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitSettings_LimiterName",
                table: "RateLimitSettings",
                column: "LimiterName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitSettings_SortOrder",
                table: "RateLimitSettings",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RateLimitSettings");
        }
    }
}
