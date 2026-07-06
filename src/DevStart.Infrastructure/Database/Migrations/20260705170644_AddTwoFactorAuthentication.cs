using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "two_factor_recovery_codes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_two_factor_recovery_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_two_factor",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    encrypted_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_timestep = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_two_factor", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_recovery_codes_user_id_code_hash",
                schema: "public",
                table: "two_factor_recovery_codes",
                columns: new[] { "user_id", "code_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_two_factor_user_id",
                schema: "public",
                table: "user_two_factor",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "two_factor_recovery_codes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user_two_factor",
                schema: "public");
        }
    }
}
