using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOrdersAndPaymentPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_payments_user_pending",
                schema: "public",
                table: "payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "subscription_id",
                schema: "public",
                table: "payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "purpose",
                schema: "public",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "service_order_id",
                schema: "public",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "service_orders",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fulfilled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_orders", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payments_service_order_id",
                schema: "public",
                table: "payments",
                column: "service_order_id");

            migrationBuilder.CreateIndex(
                name: "ux_payments_user_pending",
                schema: "public",
                table: "payments",
                column: "user_id",
                unique: true,
                filter: "status = 0 AND purpose = 0");

            migrationBuilder.CreateIndex(
                name: "ix_service_orders_user_status",
                schema: "public",
                table: "service_orders",
                columns: new[] { "user_id", "status" });

            migrationBuilder.AddForeignKey(
                name: "fk_payments_service_orders_service_order_id",
                schema: "public",
                table: "payments",
                column: "service_order_id",
                principalSchema: "public",
                principalTable: "service_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payments_service_orders_service_order_id",
                schema: "public",
                table: "payments");

            migrationBuilder.DropTable(
                name: "service_orders",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_payments_service_order_id",
                schema: "public",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "ux_payments_user_pending",
                schema: "public",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "purpose",
                schema: "public",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "service_order_id",
                schema: "public",
                table: "payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "subscription_id",
                schema: "public",
                table: "payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_payments_user_pending",
                schema: "public",
                table: "payments",
                column: "user_id",
                unique: true,
                filter: "status = 0");
        }
    }
}
