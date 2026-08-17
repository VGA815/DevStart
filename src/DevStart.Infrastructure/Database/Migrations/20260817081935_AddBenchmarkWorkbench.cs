using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBenchmarkWorkbench : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "benchmark_industry_mapping",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_kind = table.Column<int>(type: "integer", nullable: false),
                    external_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    industry = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_industry_mapping", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "benchmark_issuer",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    inn = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    industry = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    revenue_override = table.Column<decimal>(type: "numeric(20,2)", nullable: true),
                    revenue_override_fiscal_year = table.Column<int>(type: "integer", nullable: true),
                    revenue_override_note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_issuer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "benchmark_observation",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    metric = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(24,4)", nullable: false),
                    as_of = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: true),
                    dataset_region = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    origin_note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_observation", x => x.id);
                    table.ForeignKey(
                        name: "fk_benchmark_observation_benchmark_issuer_issuer_id",
                        column: x => x.issuer_id,
                        principalSchema: "public",
                        principalTable: "benchmark_issuer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_issuer_industry_active",
                schema: "public",
                table: "benchmark_issuer",
                columns: new[] { "industry", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_benchmark_issuer_ticker",
                schema: "public",
                table: "benchmark_issuer",
                column: "ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_observation_issuer_id",
                schema: "public",
                table: "benchmark_observation",
                column: "issuer_id");

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_observation_metric_as_of",
                schema: "public",
                table: "benchmark_observation",
                columns: new[] { "metric", "as_of" });

            migrationBuilder.CreateIndex(
                name: "ux_benchmark_observation_key",
                schema: "public",
                table: "benchmark_observation",
                columns: new[] { "source", "issuer_id", "external_key", "dataset_region", "metric", "as_of" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            // The natural key of a mapping is (source_kind, external_key) compared case-insensitively:
            // every reader builds its lookup with OrdinalIgnoreCase, so two rows differing only in
            // casing would make those reads throw on a duplicate key. PostgreSQL compares text
            // case-sensitively and EF Core has no expression-index API, so the constraint is written
            // by hand here. Dropped implicitly with the table in Down.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX ux_benchmark_industry_mapping_source_key
                    ON public.benchmark_industry_mapping (source_kind, lower(external_key));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "benchmark_industry_mapping",
                schema: "public");

            migrationBuilder.DropTable(
                name: "benchmark_observation",
                schema: "public");

            migrationBuilder.DropTable(
                name: "benchmark_issuer",
                schema: "public");
        }
    }
}
