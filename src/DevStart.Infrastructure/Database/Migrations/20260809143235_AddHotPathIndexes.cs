using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddHotPathIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_startup_competitors_startup_id",
                schema: "public",
                table: "startup_competitors");

            migrationBuilder.CreateIndex(
                name: "ix_startup_members_startup_role",
                schema: "public",
                table: "startup_members",
                columns: new[] { "startup_id", "role" });

            migrationBuilder.CreateIndex(
                name: "ix_startup_investors_startup",
                schema: "public",
                table: "startup_investors",
                column: "startup_id");

            migrationBuilder.CreateIndex(
                name: "ix_startup_followers_startup",
                schema: "public",
                table: "startup_followers",
                column: "startup_id");

            migrationBuilder.CreateIndex(
                name: "ix_startup_document_files_startup",
                schema: "public",
                table: "startup_document_files",
                column: "startup_id");

            migrationBuilder.CreateIndex(
                name: "ix_startup_document_files_uploader",
                schema: "public",
                table: "startup_document_files",
                column: "uploader_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_type_reference_created",
                schema: "public",
                table: "notifications",
                columns: new[] { "type", "reference_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_startup_members_startup_role",
                schema: "public",
                table: "startup_members");

            migrationBuilder.DropIndex(
                name: "ix_startup_investors_startup",
                schema: "public",
                table: "startup_investors");

            migrationBuilder.DropIndex(
                name: "ix_startup_followers_startup",
                schema: "public",
                table: "startup_followers");

            migrationBuilder.DropIndex(
                name: "ix_startup_document_files_startup",
                schema: "public",
                table: "startup_document_files");

            migrationBuilder.DropIndex(
                name: "ix_startup_document_files_uploader",
                schema: "public",
                table: "startup_document_files");

            migrationBuilder.DropIndex(
                name: "ix_notifications_type_reference_created",
                schema: "public",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "ix_startup_competitors_startup_id",
                schema: "public",
                table: "startup_competitors",
                column: "startup_id");
        }
    }
}
