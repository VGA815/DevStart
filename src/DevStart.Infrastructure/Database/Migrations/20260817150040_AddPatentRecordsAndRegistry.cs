using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPatentRecordsAndRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "inn",
                schema: "public",
                table: "startups",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ogrn",
                schema: "public",
                table: "startups",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "patent_registry_entries",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    number_normalized = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    holder_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    holder_inn = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    registered_on = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    source_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patent_registry_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "startup_patents",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    number_raw = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    number_normalized = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_startup_patents", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patent_registry_entries_holder_inn",
                schema: "public",
                table: "patent_registry_entries",
                column: "holder_inn",
                filter: "holder_inn IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_patent_registry_entries_kind_number",
                schema: "public",
                table: "patent_registry_entries",
                columns: new[] { "kind", "number_normalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_startup_patents_startup_kind_number",
                schema: "public",
                table: "startup_patents",
                columns: new[] { "startup_id", "kind", "number_normalized" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patent_registry_entries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "startup_patents",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "inn",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "ogrn",
                schema: "public",
                table: "startups");
        }
    }
}
