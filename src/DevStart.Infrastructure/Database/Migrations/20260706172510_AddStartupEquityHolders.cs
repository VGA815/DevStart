using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStartupEquityHolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "startup_equity_holders",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    holder_type = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    equity_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    vesting_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    vesting_months = table.Column<int>(type: "integer", nullable: true),
                    cliff_months = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_startup_equity_holders", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_startup_equity_holders_startup_id",
                schema: "public",
                table: "startup_equity_holders",
                column: "startup_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "startup_equity_holders",
                schema: "public");
        }
    }
}
