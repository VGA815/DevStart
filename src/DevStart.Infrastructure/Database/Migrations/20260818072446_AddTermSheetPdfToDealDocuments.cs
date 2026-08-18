using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <summary>
    /// Adds the PDF term sheet's storage key and content hash.
    /// <para>
    /// Both columns are NOT NULL with an empty-string default, which is what rows written before PDF
    /// generation get. That empty key is a meaningful state rather than a gap: the generation job
    /// treats such a row as incomplete and fills it in on the next run, so no deal is left with a
    /// document set that can never gain a PDF. New rows always carry both values.
    /// </para>
    /// </summary>
    public partial class AddTermSheetPdfToDealDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "term_sheet_pdf_object_key",
                schema: "public",
                table: "deal_documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "term_sheet_pdf_sha256",
                schema: "public",
                table: "deal_documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "term_sheet_pdf_object_key",
                schema: "public",
                table: "deal_documents");

            migrationBuilder.DropColumn(
                name: "term_sheet_pdf_sha256",
                schema: "public",
                table: "deal_documents");
        }
    }
}
