using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddValuationInputsAndSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_strategic_partnerships",
                schema: "public",
                table: "startups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "industry",
                schema: "public",
                table: "startups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "target_round_amount",
                schema: "public",
                table: "startups",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "startup_valuation_snapshots",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_score = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    team_score = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    market_score = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    product_score = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    traction_score = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    competition_score = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    valuation_low = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valuation_high = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valuation_point = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    methods_used = table.Column<string>(type: "text", nullable: false),
                    breakdown_json = table.Column<string>(type: "jsonb", nullable: true),
                    methodology_version = table.Column<string>(type: "text", nullable: false),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_startup_valuation_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_startup_valuation_snapshots_startup_calculated",
                schema: "public",
                table: "startup_valuation_snapshots",
                columns: new[] { "startup_id", "calculated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "startup_valuation_snapshots",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "has_strategic_partnerships",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "industry",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "target_round_amount",
                schema: "public",
                table: "startups");
        }
    }
}
