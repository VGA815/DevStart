using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class CompletePaymentPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payments_provider_payment",
                schema: "public",
                table: "payments");

            migrationBuilder.AlterColumn<string>(
                name: "provider_payment_id",
                schema: "public",
                table: "payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "confirmation_url",
                schema: "public",
                table: "payments",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_provider_payment",
                schema: "public",
                table: "payments",
                columns: new[] { "provider", "provider_payment_id" },
                unique: true,
                filter: "provider_payment_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_payments_subscriptions_subscription_id",
                schema: "public",
                table: "payments",
                column: "subscription_id",
                principalSchema: "public",
                principalTable: "subscriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payments_subscriptions_subscription_id",
                schema: "public",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "ix_payments_provider_payment",
                schema: "public",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "confirmation_url",
                schema: "public",
                table: "payments");

            migrationBuilder.AlterColumn<string>(
                name: "provider_payment_id",
                schema: "public",
                table: "payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_provider_payment",
                schema: "public",
                table: "payments",
                columns: new[] { "provider", "provider_payment_id" });
        }
    }
}
