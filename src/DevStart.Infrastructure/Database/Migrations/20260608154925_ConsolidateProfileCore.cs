using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateProfileCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the canonical personal social-link columns to profiles.
            migrationBuilder.AddColumn<string>(
                name: "github_url",
                schema: "public",
                table: "profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "linkedin_url",
                schema: "public",
                table: "profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telegram_url",
                schema: "public",
                table: "profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "twitter_url",
                schema: "public",
                table: "profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // 2. Backfill personal data from role profiles into the shared profile
            //    (only where the profile does not already carry the value).
            migrationBuilder.Sql(@"
                UPDATE public.profiles AS p
                SET name = COALESCE(NULLIF(p.name, ''), ep.display_name),
                    bio = COALESCE(p.bio, ep.bio),
                    url = COALESCE(p.url, ep.website),
                    linkedin_url = COALESCE(p.linkedin_url, ep.linkedin_url),
                    twitter_url = COALESCE(p.twitter_url, ep.twitter_url),
                    github_url = COALESCE(p.github_url, ep.github_url),
                    telegram_url = COALESCE(p.telegram_url, ep.telegram_url),
                    is_public = p.is_public OR ep.is_public
                FROM public.expert_profiles AS ep
                WHERE ep.user_id = p.user_id;");

            migrationBuilder.Sql(@"
                UPDATE public.profiles AS p
                SET name = COALESCE(NULLIF(p.name, ''), ip.display_name),
                    bio = COALESCE(p.bio, ip.bio),
                    url = COALESCE(p.url, ip.website),
                    is_public = p.is_public OR ip.is_public
                FROM public.investor_profiles AS ip
                WHERE ip.user_id = p.user_id;");

            // Best-effort: carry a member's bio onto the shared profile if it has none
            // (most recently updated membership wins).
            migrationBuilder.Sql(@"
                UPDATE public.profiles AS p
                SET bio = sm.bio
                FROM (
                    SELECT DISTINCT ON (profile_id) profile_id, bio
                    FROM public.startup_members
                    WHERE bio IS NOT NULL
                    ORDER BY profile_id, updated_at DESC
                ) AS sm
                WHERE sm.profile_id = p.user_id AND p.bio IS NULL;");

            // 3. Drop the now-duplicated columns from the role tables.
            migrationBuilder.DropColumn(
                name: "bio",
                schema: "public",
                table: "startup_members");

            migrationBuilder.DropColumn(
                name: "bio",
                schema: "public",
                table: "investor_profiles");

            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "public",
                table: "investor_profiles");

            migrationBuilder.DropColumn(
                name: "is_public",
                schema: "public",
                table: "investor_profiles");

            migrationBuilder.DropColumn(
                name: "website",
                schema: "public",
                table: "investor_profiles");

            migrationBuilder.DropColumn(
                name: "bio",
                schema: "public",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "public",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "github_url",
                schema: "public",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "is_public",
                schema: "public",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "linkedin_url",
                schema: "public",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "telegram_url",
                schema: "public",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "twitter_url",
                schema: "public",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "website",
                schema: "public",
                table: "expert_profiles");

            // 4. Add the foreign keys from role tables to the shared profile.
            migrationBuilder.AddForeignKey(
                name: "fk_expert_profiles_profiles_user_id",
                schema: "public",
                table: "expert_profiles",
                column: "user_id",
                principalSchema: "public",
                principalTable: "profiles",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_investor_profiles_profiles_user_id",
                schema: "public",
                table: "investor_profiles",
                column: "user_id",
                principalSchema: "public",
                principalTable: "profiles",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_startup_members_profiles_profile_id",
                schema: "public",
                table: "startup_members",
                column: "profile_id",
                principalSchema: "public",
                principalTable: "profiles",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expert_profiles_profiles_user_id",
                schema: "public",
                table: "expert_profiles");

            migrationBuilder.DropForeignKey(
                name: "fk_investor_profiles_profiles_user_id",
                schema: "public",
                table: "investor_profiles");

            migrationBuilder.DropForeignKey(
                name: "fk_startup_members_profiles_profile_id",
                schema: "public",
                table: "startup_members");

            migrationBuilder.DropColumn(
                name: "github_url",
                schema: "public",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "linkedin_url",
                schema: "public",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "telegram_url",
                schema: "public",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "twitter_url",
                schema: "public",
                table: "profiles");

            migrationBuilder.AddColumn<string>(
                name: "bio",
                schema: "public",
                table: "startup_members",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bio",
                schema: "public",
                table: "investor_profiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "public",
                table: "investor_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                schema: "public",
                table: "investor_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "website",
                schema: "public",
                table: "investor_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bio",
                schema: "public",
                table: "expert_profiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "public",
                table: "expert_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "github_url",
                schema: "public",
                table: "expert_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                schema: "public",
                table: "expert_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "linkedin_url",
                schema: "public",
                table: "expert_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telegram_url",
                schema: "public",
                table: "expert_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "twitter_url",
                schema: "public",
                table: "expert_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website",
                schema: "public",
                table: "expert_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
