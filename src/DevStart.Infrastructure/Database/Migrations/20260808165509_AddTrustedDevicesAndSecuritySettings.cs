using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustedDevicesAndSecuritySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_used_at",
                schema: "public",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "session_id",
                schema: "public",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "session_started_at",
                schema: "public",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Existing rows predate session tracking: treat each one as its own session root, which is
            // what it effectively was — rotation history is not reconstructable, and every row still
            // active is the head of its own chain anyway.
            migrationBuilder.Sql("""
                UPDATE public.refresh_tokens
                SET session_id = id,
                    session_started_at = created_at,
                    last_used_at = created_at;
                """);

            // The sentinel defaults above exist only to make the columns addable as NOT NULL; the
            // application always supplies real values, so drop them rather than leave a trap behind.
            migrationBuilder.Sql("""
                ALTER TABLE public.refresh_tokens
                    ALTER COLUMN session_id DROP DEFAULT,
                    ALTER COLUMN session_started_at DROP DEFAULT,
                    ALTER COLUMN last_used_at DROP DEFAULT;
                """);

            migrationBuilder.CreateTable(
                name: "trusted_devices",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_seen_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trusted_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_security_settings",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strictness = table.Column<int>(type: "integer", nullable: false),
                    trust_duration_days = table.Column<int>(type: "integer", nullable: false),
                    notify_on_new_device_login = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_security_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_session_id",
                schema: "public",
                table: "refresh_tokens",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_expires_at",
                schema: "public",
                table: "trusted_devices",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_token_hash",
                schema: "public",
                table: "trusted_devices",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_user_id",
                schema: "public",
                table: "trusted_devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_security_settings_user_id",
                schema: "public",
                table: "user_security_settings",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trusted_devices",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user_security_settings",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_session_id",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "last_used_at",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "session_id",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "session_started_at",
                schema: "public",
                table: "refresh_tokens");
        }
    }
}
