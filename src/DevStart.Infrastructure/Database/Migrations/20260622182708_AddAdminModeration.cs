using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ban_expires_at",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ban_reason",
                schema: "public",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "banned_at",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "banned_by_user_id",
                schema: "public",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_banned",
                schema: "public",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "source",
                schema: "public",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ban_expires_at",
                schema: "public",
                table: "startups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ban_reason",
                schema: "public",
                table: "startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "banned_at",
                schema: "public",
                table: "startups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "banned_by_user_id",
                schema: "public",
                table: "startups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_banned",
                schema: "public",
                table: "startups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                schema: "public",
                table: "payments",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "promo_code_id",
                schema: "public",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admin_action_logs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_type = table.Column<int>(type: "integer", nullable: false),
                    target_type = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_action_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promo_codes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    free_period_days = table.Column<int>(type: "integer", nullable: true),
                    plan = table.Column<int>(type: "integer", nullable: false),
                    max_redemptions = table.Column<int>(type: "integer", nullable: true),
                    redeemed_count = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promo_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promo_code_redemptions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    promo_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_applied = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    redeemed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promo_code_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_promo_code_redemptions_promo_codes_promo_code_id",
                        column: x => x.promo_code_id,
                        principalSchema: "public",
                        principalTable: "promo_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_banned",
                schema: "public",
                table: "users",
                columns: new[] { "is_banned", "ban_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_startups_banned",
                schema: "public",
                table: "startups",
                columns: new[] { "is_banned", "ban_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_promo_code_id",
                schema: "public",
                table: "payments",
                column: "promo_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_admin_action_logs_admin_user_id",
                schema: "public",
                table: "admin_action_logs",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_admin_action_logs_created_at",
                schema: "public",
                table: "admin_action_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_admin_action_logs_target",
                schema: "public",
                table: "admin_action_logs",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ux_promo_code_redemptions_promo_user",
                schema: "public",
                table: "promo_code_redemptions",
                columns: new[] { "promo_code_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_promo_codes_code",
                schema: "public",
                table: "promo_codes",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_payments_promo_codes_promo_code_id",
                schema: "public",
                table: "payments",
                column: "promo_code_id",
                principalSchema: "public",
                principalTable: "promo_codes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payments_promo_codes_promo_code_id",
                schema: "public",
                table: "payments");

            migrationBuilder.DropTable(
                name: "admin_action_logs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "promo_code_redemptions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "promo_codes",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_users_banned",
                schema: "public",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_startups_banned",
                schema: "public",
                table: "startups");

            migrationBuilder.DropIndex(
                name: "ix_payments_promo_code_id",
                schema: "public",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ban_expires_at",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ban_reason",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "banned_at",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "banned_by_user_id",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_banned",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "public",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ban_expires_at",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "ban_reason",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "banned_at",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "banned_by_user_id",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "is_banned",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                schema: "public",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "promo_code_id",
                schema: "public",
                table: "payments");
        }
    }
}
