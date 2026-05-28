using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSinglePendingExpertCollaborationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_expert_collaboration_requests_expert_startup_pending",
                schema: "public",
                table: "expert_collaboration_requests",
                columns: new[] { "expert_profile_id", "startup_id" },
                unique: true,
                filter: "status = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_expert_collaboration_requests_expert_startup_pending",
                schema: "public",
                table: "expert_collaboration_requests");
        }
    }
}
