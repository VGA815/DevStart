using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "target_amount",
                schema: "public",
                table: "startup_roadmap_items",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "investment_applications",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    roadmap_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investment_applications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "investment_deals",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    investor_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    roadmap_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    confirmed_by_startup = table.Column<bool>(type: "boolean", nullable: false),
                    confirmed_by_investor = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investment_deals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "investor_profiles",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investor_profiles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_investment_applications_investor_status",
                schema: "public",
                table: "investment_applications",
                columns: new[] { "investor_profile_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_investment_applications_roadmap_item_id",
                schema: "public",
                table: "investment_applications",
                column: "roadmap_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_investment_applications_startup_status",
                schema: "public",
                table: "investment_applications",
                columns: new[] { "startup_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_investment_deals_application_id",
                schema: "public",
                table: "investment_deals",
                column: "application_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investment_deals_investor_status",
                schema: "public",
                table: "investment_deals",
                columns: new[] { "investor_profile_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_investment_deals_startup_status",
                schema: "public",
                table: "investment_deals",
                columns: new[] { "startup_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_investor_profiles_type",
                schema: "public",
                table: "investor_profiles",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_investor_profiles_user_id",
                schema: "public",
                table: "investor_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "investment_applications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "investment_deals",
                schema: "public");

            migrationBuilder.DropTable(
                name: "investor_profiles",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "target_amount",
                schema: "public",
                table: "startup_roadmap_items");
        }
    }
}
