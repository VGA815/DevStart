using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOrderTargetsAndFeaturedStartups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "featured_until",
                schema: "public",
                table: "startups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                schema: "public",
                table: "service_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                schema: "public",
                table: "service_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "refunded_at",
                schema: "public",
                table: "service_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_id",
                schema: "public",
                table: "service_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_startups_featured_until",
                schema: "public",
                table: "startups",
                column: "featured_until",
                filter: "featured_until IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_service_orders_entitlement",
                schema: "public",
                table: "service_orders",
                columns: new[] { "user_id", "service_type", "target_id" },
                filter: "status = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_startups_featured_until",
                schema: "public",
                table: "startups");

            migrationBuilder.DropIndex(
                name: "ix_service_orders_entitlement",
                schema: "public",
                table: "service_orders");

            migrationBuilder.DropColumn(
                name: "featured_until",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                schema: "public",
                table: "service_orders");

            migrationBuilder.DropColumn(
                name: "expires_at",
                schema: "public",
                table: "service_orders");

            migrationBuilder.DropColumn(
                name: "refunded_at",
                schema: "public",
                table: "service_orders");

            migrationBuilder.DropColumn(
                name: "target_id",
                schema: "public",
                table: "service_orders");
        }
    }
}
