using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddChatFilesAndMessageDocumentAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "document_ids",
                schema: "public",
                table: "messages",
                type: "jsonb",
                nullable: false,
                // Empty JSON array, not "": '' is not valid jsonb and would fail on existing rows.
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "file_ids",
                schema: "public",
                table: "messages",
                type: "jsonb",
                nullable: false,
                // Empty JSON array, not "": '' is not valid jsonb and would fail on existing rows.
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "chat_files",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    object_name = table.Column<string>(type: "text", nullable: false),
                    bucket = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    upload_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_files", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_files_message_id",
                schema: "public",
                table: "chat_files",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_files_uploader_id",
                schema: "public",
                table: "chat_files",
                column: "uploader_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_files",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "document_ids",
                schema: "public",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "file_ids",
                schema: "public",
                table: "messages");
        }
    }
}
