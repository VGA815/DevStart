using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar_url",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "file_url",
                schema: "public",
                table: "startup_document_files");

            migrationBuilder.DropColumn(
                name: "avatar_url",
                schema: "public",
                table: "profiles");

            migrationBuilder.AlterColumn<string>(
                name: "url",
                schema: "public",
                table: "startups",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "social_media_links",
                schema: "public",
                table: "startups",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<int>(
                name: "location",
                schema: "public",
                table: "startups",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "public",
                table: "startups",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "billing_email",
                schema: "public",
                table: "startups",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "avatar_id",
                schema: "public",
                table: "startups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "value_proposition",
                schema: "public",
                table: "startup_products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "stack",
                schema: "public",
                table: "startup_products",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "problem",
                schema: "public",
                table: "startup_products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "differentiators",
                schema: "public",
                table: "startup_products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "file_id",
                schema: "public",
                table: "startup_document_files",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "avatar_id",
                schema: "public",
                table: "profiles",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar_id",
                schema: "public",
                table: "startups");

            migrationBuilder.DropColumn(
                name: "file_id",
                schema: "public",
                table: "startup_document_files");

            migrationBuilder.DropColumn(
                name: "avatar_id",
                schema: "public",
                table: "profiles");

            migrationBuilder.AlterColumn<string>(
                name: "url",
                schema: "public",
                table: "startups",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "social_media_links",
                schema: "public",
                table: "startups",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "location",
                schema: "public",
                table: "startups",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "public",
                table: "startups",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "billing_email",
                schema: "public",
                table: "startups",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "avatar_url",
                schema: "public",
                table: "startups",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "value_proposition",
                schema: "public",
                table: "startup_products",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "stack",
                schema: "public",
                table: "startup_products",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "problem",
                schema: "public",
                table: "startup_products",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "differentiators",
                schema: "public",
                table: "startup_products",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_url",
                schema: "public",
                table: "startup_document_files",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "avatar_url",
                schema: "public",
                table: "profiles",
                type: "text",
                nullable: true);
        }
    }
}
