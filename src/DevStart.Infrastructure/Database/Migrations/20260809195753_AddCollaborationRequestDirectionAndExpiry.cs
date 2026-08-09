using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCollaborationRequestDirectionAndExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "initiator",
                schema: "public",
                table: "expert_collaboration_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_expert_collaboration_requests_pending_created_at",
                schema: "public",
                table: "expert_collaboration_requests",
                column: "created_at",
                filter: "status = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_expert_collaboration_requests_pending_created_at",
                schema: "public",
                table: "expert_collaboration_requests");

            migrationBuilder.DropColumn(
                name: "initiator",
                schema: "public",
                table: "expert_collaboration_requests");
        }
    }
}
