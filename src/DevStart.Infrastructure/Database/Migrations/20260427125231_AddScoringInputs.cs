using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_patents",
                schema: "public",
                table: "startups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "market_growth_rate",
                schema: "public",
                table: "startups",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_prior_exit",
                schema: "public",
                table: "startup_members",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "previous_startups_count",
                schema: "public",
                table: "startup_members",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_patents",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "market_growth_rate",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "has_prior_exit",
                schema: "public",
                table: "startup_members");

            migrationBuilder.DropColumn(
                name: "previous_startups_count",
                schema: "public",
                table: "startup_members");
        }
    }
}
