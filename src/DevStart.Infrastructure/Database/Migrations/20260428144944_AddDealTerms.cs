using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDealTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "discount",
                schema: "public",
                table: "investment_deals",
                type: "numeric(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "instrument",
                schema: "public",
                table: "investment_deals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "interest_rate",
                schema: "public",
                table: "investment_deals",
                type: "numeric(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "liquidation_preference",
                schema: "public",
                table: "investment_deals",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pre_money_valuation",
                schema: "public",
                table: "investment_deals",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "pro_rata_rights",
                schema: "public",
                table: "investment_deals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "term_months",
                schema: "public",
                table: "investment_deals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valuation_cap",
                schema: "public",
                table: "investment_deals",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount",
                schema: "public",
                table: "investment_applications",
                type: "numeric(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "instrument",
                schema: "public",
                table: "investment_applications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "interest_rate",
                schema: "public",
                table: "investment_applications",
                type: "numeric(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "liquidation_preference",
                schema: "public",
                table: "investment_applications",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pre_money_valuation",
                schema: "public",
                table: "investment_applications",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "pro_rata_rights",
                schema: "public",
                table: "investment_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "term_months",
                schema: "public",
                table: "investment_applications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valuation_cap",
                schema: "public",
                table: "investment_applications",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "discount",
                schema: "public",
                table: "investment_deals");

            migrationBuilder.DropColumn(
                name: "instrument",
                schema: "public",
                table: "investment_deals");

            migrationBuilder.DropColumn(
                name: "interest_rate",
                schema: "public",
                table: "investment_deals");

            migrationBuilder.DropColumn(
                name: "liquidation_preference",
                schema: "public",
                table: "investment_deals");

            migrationBuilder.DropColumn(
                name: "pre_money_valuation",
                schema: "public",
                table: "investment_deals");

            migrationBuilder.DropColumn(
                name: "pro_rata_rights",
                schema: "public",
                table: "investment_deals");

            migrationBuilder.DropColumn(
                name: "term_months",
                schema: "public",
                table: "investment_deals");

            migrationBuilder.DropColumn(
                name: "valuation_cap",
                schema: "public",
                table: "investment_deals");

            migrationBuilder.DropColumn(
                name: "discount",
                schema: "public",
                table: "investment_applications");

            migrationBuilder.DropColumn(
                name: "instrument",
                schema: "public",
                table: "investment_applications");

            migrationBuilder.DropColumn(
                name: "interest_rate",
                schema: "public",
                table: "investment_applications");

            migrationBuilder.DropColumn(
                name: "liquidation_preference",
                schema: "public",
                table: "investment_applications");

            migrationBuilder.DropColumn(
                name: "pre_money_valuation",
                schema: "public",
                table: "investment_applications");

            migrationBuilder.DropColumn(
                name: "pro_rata_rights",
                schema: "public",
                table: "investment_applications");

            migrationBuilder.DropColumn(
                name: "term_months",
                schema: "public",
                table: "investment_applications");

            migrationBuilder.DropColumn(
                name: "valuation_cap",
                schema: "public",
                table: "investment_applications");
        }
    }
}
