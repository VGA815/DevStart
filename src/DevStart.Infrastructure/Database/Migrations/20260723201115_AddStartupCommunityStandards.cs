using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStartupCommunityStandards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "startup_community_documents",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_startup_community_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "startup_community_standards",
                schema: "public",
                columns: table => new
                {
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_count = table.Column<int>(type: "integer", nullable: false),
                    total_count = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_startup_community_standards", x => x.startup_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_startup_community_documents_startup_id_type",
                schema: "public",
                table: "startup_community_documents",
                columns: new[] { "startup_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_startup_community_standards_level",
                schema: "public",
                table: "startup_community_standards",
                column: "level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "startup_community_documents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "startup_community_standards",
                schema: "public");
        }
    }
}
