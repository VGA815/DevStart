using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddExpertProfilesFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expert_experiences",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    expert_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expert_experiences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expert_profile_specializations",
                schema: "public",
                columns: table => new
                {
                    expert_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    specialization = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expert_profile_specializations", x => new { x.expert_profile_id, x.specialization });
                });

            migrationBuilder.CreateTable(
                name: "expert_profiles",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    linkedin_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    twitter_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    github_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    telegram_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expert_profiles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expert_experiences_expert_profile_id",
                schema: "public",
                table: "expert_experiences",
                column: "expert_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_expert_profile_specializations_specialization",
                schema: "public",
                table: "expert_profile_specializations",
                column: "specialization");

            migrationBuilder.CreateIndex(
                name: "ix_expert_profiles_user_id",
                schema: "public",
                table: "expert_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expert_experiences",
                schema: "public");

            migrationBuilder.DropTable(
                name: "expert_profile_specializations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "expert_profiles",
                schema: "public");
        }
    }
}
