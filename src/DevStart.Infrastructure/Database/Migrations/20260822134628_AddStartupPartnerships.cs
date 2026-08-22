using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// М3: the strategic-partnerships checkbox becomes a list of records.
    ///
    /// <c>has_strategic_partnerships</c> is dropped rather than kept beside the new table. The old
    /// value is not migrated because there is nothing to migrate it into — a boolean carries no
    /// partner, no website and no account of the arrangement, and inventing a placeholder record from
    /// it would hand the startup a third of the Berkus ceiling for a checkbox all over again. Startups
    /// that had the box ticked start with an empty list and describe the partnerships they actually
    /// have; the down migration restores the column but not the ticks.
    /// </summary>
    public partial class AddStartupPartnerships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_strategic_partnerships",
                schema: "public",
                table: "startups");

            migrationBuilder.CreateTable(
                name: "startup_partnerships",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    website = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    normalized_domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_startup_partnerships", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_startup_partnerships_startup_domain",
                schema: "public",
                table: "startup_partnerships",
                columns: new[] { "startup_id", "normalized_domain" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "startup_partnerships",
                schema: "public");

            migrationBuilder.AddColumn<bool>(
                name: "has_strategic_partnerships",
                schema: "public",
                table: "startups",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
