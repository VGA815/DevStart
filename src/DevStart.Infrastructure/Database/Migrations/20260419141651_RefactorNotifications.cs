using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevStart.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RefactorNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE public.notifications
                ALTER COLUMN type TYPE integer
                USING (
                    CASE type
                        WHEN 'Welcome' THEN 0
                        WHEN 'EmailVerified' THEN 1
                        WHEN 'MessageReceived' THEN 2
                        ELSE 0
                    END
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE public.notifications
                ALTER COLUMN type TYPE character varying(50)
                USING (
                    CASE type
                        WHEN 0 THEN 'Welcome'
                        WHEN 1 THEN 'EmailVerified'
                        WHEN 2 THEN 'MessageReceived'
                        ELSE 'Welcome'
                    END
                );
            ");
        }
    }
}
