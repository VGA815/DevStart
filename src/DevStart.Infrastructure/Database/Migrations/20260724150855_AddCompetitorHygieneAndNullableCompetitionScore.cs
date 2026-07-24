using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitorHygieneAndNullableCompetitionScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "competition_score",
                schema: "public",
                table: "startup_valuation_snapshots",
                type: "numeric(6,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,2)");

            migrationBuilder.AddColumn<string>(
                name: "normalized_domain",
                schema: "public",
                table: "startup_competitors",
                type: "character varying(253)",
                maxLength: 253,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_startup_competitors_startup_domain",
                schema: "public",
                table: "startup_competitors",
                columns: new[] { "startup_id", "normalized_domain" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_startup_competitors_startup_domain",
                schema: "public",
                table: "startup_competitors");

            migrationBuilder.DropColumn(
                name: "normalized_domain",
                schema: "public",
                table: "startup_competitors");

            migrationBuilder.AlterColumn<decimal>(
                name: "competition_score",
                schema: "public",
                table: "startup_valuation_snapshots",
                type: "numeric(6,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,2)",
                oldNullable: true);
        }
    }
}
