using DevStart.Domain.EmailVerificationTokens;
using DevStart.Domain.MediaFiles;
using DevStart.Domain.Profiles;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupFollowers;
using DevStart.Domain.StartupInvestors;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.StartupProducts;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Startups;
using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Abstractions.Data
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<UserPreference> Preferences { get; }
        DbSet<Startup> Startups { get; }
        DbSet<StartupRoadmapItem> StartupRoadmapItems { get; }
        DbSet<StartupProduct> StartupProducts { get; }
        DbSet<StartupMetric> StartupMetrics { get; }
        DbSet<StartupMember> StartupMembers { get; }
        DbSet<StartupInvestor> StartupInvestors { get; }
        DbSet<StartupFollower> StartupFollowers { get; }
        DbSet<StartupDocumentFile> StartupDocumentsFiles { get; }
        DbSet<Profile> Profiles { get; }
        DbSet<MediaFile> MediaFiles { get; }
        DbSet<EmailVerificationToken> EmailVerificationTokens { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
