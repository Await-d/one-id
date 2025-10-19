using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OneID.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginPolicyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IpAccessRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetRoleName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpAccessRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginTimeRestrictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetRoleName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AllowedDaysOfWeek = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DailyStartTime = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    DailyEndTime = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginTimeRestrictions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IpAccessRules_IsEnabled",
                table: "IpAccessRules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_IpAccessRules_Priority",
                table: "IpAccessRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_IpAccessRules_RuleType_IsEnabled",
                table: "IpAccessRules",
                columns: new[] { "RuleType", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginTimeRestrictions_IsEnabled",
                table: "LoginTimeRestrictions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_LoginTimeRestrictions_Priority",
                table: "LoginTimeRestrictions",
                column: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpAccessRules");

            migrationBuilder.DropTable(
                name: "LoginTimeRestrictions");
        }
    }
}
