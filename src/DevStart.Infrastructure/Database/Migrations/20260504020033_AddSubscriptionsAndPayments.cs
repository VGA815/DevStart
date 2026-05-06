using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionsAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payments",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<int>(type: "integer", nullable: false),
                    provider_payment_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payments_provider_payment",
                schema: "public",
                table: "payments",
                columns: new[] { "provider", "provider_payment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_subscription_id",
                schema: "public",
                table: "payments",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_user_status",
                schema: "public",
                table: "payments",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_expires_at",
                schema: "public",
                table: "subscriptions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_user_status",
                schema: "public",
                table: "subscriptions",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "public");
        }
    }
}
