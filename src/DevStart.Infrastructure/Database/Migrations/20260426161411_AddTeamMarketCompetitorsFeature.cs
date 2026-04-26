using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMarketCompetitorsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "sam",
                schema: "public",
                table: "startups",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "som",
                schema: "public",
                table: "startups",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tam",
                schema: "public",
                table: "startups",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bio",
                schema: "public",
                table: "startup_members",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "position",
                schema: "public",
                table: "startup_members",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "years_of_experience",
                schema: "public",
                table: "startup_members",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "startup_competitors",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    website = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    strengths_vs_us = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    weaknesses_vs_us = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_startup_competitors", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_startup_competitors_startup_id",
                schema: "public",
                table: "startup_competitors",
                column: "startup_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "startup_competitors",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "sam",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "som",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "tam",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "bio",
                schema: "public",
                table: "startup_members");

            migrationBuilder.DropColumn(
                name: "position",
                schema: "public",
                table: "startup_members");

            migrationBuilder.DropColumn(
                name: "years_of_experience",
                schema: "public",
                table: "startup_members");
        }
    }
}
