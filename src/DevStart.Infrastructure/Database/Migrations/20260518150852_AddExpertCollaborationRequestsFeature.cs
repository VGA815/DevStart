using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddExpertCollaborationRequestsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expert_collaboration_requests",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    expert_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    startup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    collaboration_type = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    proposed_hours_per_week = table.Column<int>(type: "integer", nullable: true),
                    proposed_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expert_collaboration_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expert_collaboration_requests_expert_status",
                schema: "public",
                table: "expert_collaboration_requests",
                columns: new[] { "expert_profile_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_expert_collaboration_requests_startup_status",
                schema: "public",
                table: "expert_collaboration_requests",
                columns: new[] { "startup_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expert_collaboration_requests",
                schema: "public");
        }
    }
}
