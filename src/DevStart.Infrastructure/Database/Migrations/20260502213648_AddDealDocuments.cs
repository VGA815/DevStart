using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDealDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deal_documents",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_sheet_object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cap_table_object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deal_documents", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deal_documents_deal_id",
                schema: "public",
                table: "deal_documents",
                column: "deal_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deal_documents",
                schema: "public");
        }
    }
}
