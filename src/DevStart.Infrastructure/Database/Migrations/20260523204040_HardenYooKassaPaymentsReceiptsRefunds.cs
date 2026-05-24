using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class HardenYooKassaPaymentsReceiptsRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "renewal_reminder_sent_at",
                schema: "public",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                schema: "public",
                table: "payments",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_status_expires",
                schema: "public",
                table: "subscriptions",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_status_created",
                schema: "public",
                table: "payments",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subscriptions_status_expires",
                schema: "public",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_payments_status_created",
                schema: "public",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "renewal_reminder_sent_at",
                schema: "public",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                schema: "public",
                table: "payments");
        }
    }
}
