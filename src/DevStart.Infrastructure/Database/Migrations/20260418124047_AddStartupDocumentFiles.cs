using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStartupDocumentFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name",
                schema: "public",
                table: "startup_document_files",
                newName: "object_name");

            migrationBuilder.RenameColumn(
                name: "file_type",
                schema: "public",
                table: "startup_document_files",
                newName: "document_type");

            migrationBuilder.RenameColumn(
                name: "file_id",
                schema: "public",
                table: "startup_document_files",
                newName: "uploader_id");

            migrationBuilder.AddColumn<string>(
                name: "bucket",
                schema: "public",
                table: "startup_document_files",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_name",
                schema: "public",
                table: "startup_document_files",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "file_size",
                schema: "public",
                table: "startup_document_files",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bucket",
                schema: "public",
                table: "startup_document_files");

            migrationBuilder.DropColumn(
                name: "document_name",
                schema: "public",
                table: "startup_document_files");

            migrationBuilder.DropColumn(
                name: "file_size",
                schema: "public",
                table: "startup_document_files");

            migrationBuilder.RenameColumn(
                name: "uploader_id",
                schema: "public",
                table: "startup_document_files",
                newName: "file_id");

            migrationBuilder.RenameColumn(
                name: "object_name",
                schema: "public",
                table: "startup_document_files",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "document_type",
                schema: "public",
                table: "startup_document_files",
                newName: "file_type");
        }
    }
}
