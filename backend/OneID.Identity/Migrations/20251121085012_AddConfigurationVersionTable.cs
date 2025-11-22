using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OneID.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationVersionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigurationVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastChangedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationVersions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ConfigurationVersions",
                columns: new[] { "Id", "LastChangedBy", "LastUpdated", "Version" },
                values: new object[] { 1, "Initial", new DateTime(2025, 11, 21, 8, 50, 12, 231, DateTimeKind.Utc).AddTicks(5237), 1L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationVersions");
        }
    }
}
