using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Domain.AccountDeletion;
using DevStart.Domain.Admin;
using DevStart.Domain.ChatFiles;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.MediaFiles;
using DevStart.Domain.Messages;
using DevStart.Domain.Profiles;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupEquity;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace DevStart.Application.AccountDeletion
{
    /// <summary>
    /// Carries out the erasure promised by the legal documents. Two rules decide what happens to any
    /// given row:
    ///
    /// <list type="bullet">
    /// <item><b>Personal data is deleted.</b> The account, the profile, credentials and devices, the
    /// user's own correspondence and files, their applications and collaboration requests.</item>
    /// <item><b>Someone else's records are anonymized, not deleted.</b> A startup that outlives the
    /// user keeps its documents, its cap table and the messages it sent — with the departed person's
    /// id stripped out of them. Payments, subscriptions, service orders and moderation logs are kept
    /// intact: the privacy policy commits to holding them (3 years / tax records), and once the user
    /// and profile rows are gone the remaining ids identify nobody.</item>
    /// </list>
    ///
    /// Startups where this user was the only founder are erased with the account — nobody would be
    /// left who could administer or delete them.
    ///
    /// Rows are loaded and removed rather than swept with <c>ExecuteDelete</c>: one account's data is
    /// small, a single transaction covers the lot, and the whole thing stays runnable on the in-memory
    /// provider the unit tests use.
    /// </summary>
    internal sealed class AccountEraser(
        IApplicationDbContext context,
        IFileStorage fileStorage,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider,
        ILogger<AccountEraser> logger) : IAccountEraser
    {
        /// <summary>Stands in for the person in rows that survive them. Points at nobody by construction.</summary>
        private static readonly Guid AnonymousActor = Guid.Empty;

        private sealed record StoredObject(string ObjectName, string Bucket);

        public async Task<Result> EraseAsync(Guid userId, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
            {
                // Already erased (a retried job, a double-fired schedule). Nothing to undo.
                logger.LogInformation("Account {UserId} is already erased; nothing to do.", userId);
                return Result.Success();
            }

            DateTime now = dateTimeProvider.UtcNow;
            var objects = new List<StoredObject>();

            // A ban has to outlive the account it was applied to, or erasure becomes the way out of it.
            if (user.IsCurrentlyBanned(now))
            {
                context.BannedIdentities.Add(
                    BannedIdentity.Create(user.Email, user.BanExpiresAt, now));
            }

            List<Guid> orphanedStartupIds = await SoleFounderStartups
                .IdsFor(context, userId)
                .ToListAsync(cancellationToken);

            foreach (Guid startupId in orphanedStartupIds)
            {
                await EraseStartupAsync(startupId, objects, cancellationToken);
            }

            List<Guid> erasedMediaIds = await ErasePersonalDataAsync(user, objects, cancellationToken);
            await AnonymizeSurvivingReferencesAsync(userId, orphanedStartupIds, erasedMediaIds, cancellationToken);

            foreach (AccountDeletionRequest request in await context.AccountDeletionRequests
                .Where(r => r.UserId == userId && r.Status == AccountDeletionRequestStatus.Pending)
                .ToListAsync(cancellationToken))
            {
                request.Complete(now);
            }

            await context.SaveChangesAsync(cancellationToken);

            // Storage and cache come after the database commit: the legally meaningful part is the row
            // deletion, and a failure out here must not roll it back. Failures are logged loudly enough
            // for an operator to clean up the leftovers by hand.
            await DeleteObjectsAsync(objects, cancellationToken);
            await InvalidateCachesAsync(userId, orphanedStartupIds, cancellationToken);

            logger.LogInformation(
                "Erased account {UserId}: {StartupCount} sole-founder startup(s), {ObjectCount} stored object(s).",
                userId, orphanedStartupIds.Count, objects.Count);

            return Result.Success();
        }

        /// <summary>
        /// Everything that exists because this person had an account. Returns the media files it
        /// deleted, so the anonymization pass can leave them alone instead of relying on "Remove beats
        /// a property change" inside the change tracker.
        /// </summary>
        private async Task<List<Guid>> ErasePersonalDataAsync(
            User user,
            List<StoredObject> objects,
            CancellationToken cancellationToken)
        {
            Guid userId = user.Id;

            // Correspondence the user held under their own name — deleting the row removes it from both
            // inboxes, which is the point: it is as much their personal data as the other side's.
            List<Message> personalMessages = await context.Messages
                .Where(m => (m.SenderType == ChatParticipantType.User && m.SenderId == userId)
                         || (m.ReceiverType == ChatParticipantType.User && m.ReceiverId == userId))
                .ToListAsync(cancellationToken);

            List<Guid> personalMessageIds = personalMessages.ConvertAll(m => m.Id);

            List<ChatFile> chatFiles = await context.ChatFiles
                .Where(f => (f.MessageId != null && personalMessageIds.Contains(f.MessageId.Value))
                         || (f.UploaderId == userId && f.MessageId == null))
                .ToListAsync(cancellationToken);

            CollectObjects(chatFiles.Select(f => new StoredObject(f.ObjectName, f.Bucket)), objects);
            context.ChatFiles.RemoveRange(chatFiles);
            context.Messages.RemoveRange(personalMessages);

            // Investment applications the user filed as an investor, plus everything downstream of them.
            // The generated term sheets name the person, so they go too.
            List<InvestmentApplication> applications = await context.InvestmentApplications
                .Where(a => a.InvestorProfileId == userId)
                .ToListAsync(cancellationToken);

            List<InvestmentDeal> deals = await context.InvestmentDeals
                .Where(d => d.InvestorProfileId == userId)
                .ToListAsync(cancellationToken);

            await EraseDealDocumentsAsync(deals.ConvertAll(d => d.Id), objects, cancellationToken);

            context.InvestmentDeals.RemoveRange(deals);
            context.InvestmentApplications.RemoveRange(applications);

            // Avatars: the media rows the person's own profiles pointed at. Anything else they uploaded
            // belongs to a startup and is only anonymized (see AnonymizeSurvivingReferencesAsync).
            var avatarIds = new List<Guid>();

            Profile? profile = await context.Profiles
                .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            if (profile?.AvatarId is { } profileAvatarId)
            {
                avatarIds.Add(profileAvatarId);
            }

            Guid? investorAvatarId = await context.InvestorProfiles
                .Where(i => i.UserId == userId)
                .Select(i => i.AvatarId)
                .SingleOrDefaultAsync(cancellationToken);
            if (investorAvatarId is { } fundLogoId)
            {
                avatarIds.Add(fundLogoId);
            }

            var erasedMediaIds = new List<Guid>();

            if (avatarIds.Count > 0)
            {
                List<MediaFile> avatars = await context.MediaFiles
                    .Where(f => avatarIds.Contains(f.Id))
                    .ToListAsync(cancellationToken);

                CollectObjects(avatars.Select(f => new StoredObject(f.ObjectName, f.Bucket)), objects);
                context.MediaFiles.RemoveRange(avatars);
                erasedMediaIds.AddRange(avatars.Select(f => f.Id));
            }

            await RemoveWhereAsync(context.ExpertProfileSpecializations, s => s.ExpertProfileId == userId, cancellationToken);
            await RemoveWhereAsync(context.ExpertExperiences, e => e.ExpertProfileId == userId, cancellationToken);
            await RemoveWhereAsync(context.ExpertCollaborationRequests, r => r.ExpertProfileId == userId, cancellationToken);
            await RemoveWhereAsync(context.ExpertProfiles, p => p.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.InvestorProfiles, p => p.UserId == userId, cancellationToken);

            await RemoveWhereAsync(context.StartupMembers, m => m.ProfileId == userId, cancellationToken);
            await RemoveWhereAsync(context.StartupFollowers, f => f.ProfileId == userId, cancellationToken);
            await RemoveWhereAsync(context.StartupInvestors, i => i.ProfileId == userId, cancellationToken);

            await RemoveWhereAsync(context.Notifications, n => n.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.UserConsents, c => c.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.Preferences, p => p.UserId == userId, cancellationToken);

            await RemoveWhereAsync(context.RefreshTokens, t => t.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.TrustedDevices, d => d.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.UserTwoFactors, t => t.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.TwoFactorRecoveryCodes, c => c.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.UserSecuritySettings, s => s.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.ExternalLogins, l => l.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.EmailVerificationTokens, t => t.UserId == userId, cancellationToken);
            await RemoveWhereAsync(context.PasswordResetTokens, t => t.UserId == userId, cancellationToken);

            if (profile is not null)
            {
                context.Profiles.Remove(profile);
            }

            context.Users.Remove(user);

            return erasedMediaIds;
        }

        /// <summary>
        /// Strips the person out of rows that belong to someone else and therefore stay. The startups
        /// being erased are excluded — their rows are already on their way out.
        /// </summary>
        private async Task AnonymizeSurvivingReferencesAsync(
            Guid userId,
            List<Guid> erasedStartupIds,
            List<Guid> erasedMediaIds,
            CancellationToken cancellationToken)
        {
            // Messages this person sent on a startup's behalf: the startup's correspondence, not theirs.
            // Only the "who typed it" link is personal, and it is the only thing removed.
            List<Message> sentAsStartup = await context.Messages
                .Where(m => m.SentByProfileId == userId)
                .Where(m => !(m.SenderType == ChatParticipantType.Startup && erasedStartupIds.Contains(m.SenderId)))
                .ToListAsync(cancellationToken);

            foreach (Message message in sentAsStartup)
            {
                message.SentByProfileId = null;
            }

            List<Guid> survivingMessageIds = sentAsStartup.ConvertAll(m => m.Id);

            List<ChatFile> survivingFiles = await context.ChatFiles
                .Where(f => f.UploaderId == userId
                         && f.MessageId != null
                         && survivingMessageIds.Contains(f.MessageId.Value))
                .ToListAsync(cancellationToken);

            foreach (ChatFile file in survivingFiles)
            {
                file.UploaderId = AnonymousActor;
            }

            // Files and media the person uploaded into a startup that outlives them: content belongs to
            // the startup, the uploader link does not.
            List<StartupDocumentFile> documents = await context.StartupDocumentFiles
                .Where(d => d.UploaderId == userId && !erasedStartupIds.Contains(d.StartupId))
                .ToListAsync(cancellationToken);

            foreach (StartupDocumentFile document in documents)
            {
                document.UploaderId = AnonymousActor;
            }

            List<MediaFile> media = await context.MediaFiles
                .Where(f => f.UploaderId == userId && !erasedMediaIds.Contains(f.Id))
                .ToListAsync(cancellationToken);

            foreach (MediaFile file in media)
            {
                file.UploaderId = AnonymousActor;
            }

            List<StartupCommunityDocument> communityDocuments = await context.StartupCommunityDocuments
                .Where(d => d.AuthorId == userId && !erasedStartupIds.Contains(d.StartupId))
                .ToListAsync(cancellationToken);

            foreach (StartupCommunityDocument document in communityDocuments)
            {
                document.AuthorId = AnonymousActor;
            }

            // Cap-table rows cannot simply go: the holders of a startup must still add up to 100%.
            // The share stays, the person behind it does not.
            List<StartupEquityHolder> equity = await context.StartupEquityHolders
                .Where(h => h.ProfileId == userId && !erasedStartupIds.Contains(h.StartupId))
                .ToListAsync(cancellationToken);

            foreach (StartupEquityHolder holder in equity)
            {
                holder.ProfileId = null;
                holder.Name ??= "Бывший участник";
            }
        }

        /// <summary>
        /// Removes a startup and everything hanging off it. Almost nothing here is wired with database
        /// cascades, so each table has to be named — a missed one would leave rows pointing at a
        /// startup that no longer exists.
        /// </summary>
        private async Task EraseStartupAsync(
            Guid startupId,
            List<StoredObject> objects,
            CancellationToken cancellationToken)
        {
            List<StartupDocumentFile> documents = await context.StartupDocumentFiles
                .Where(d => d.StartupId == startupId)
                .ToListAsync(cancellationToken);

            CollectObjects(documents.Select(d => new StoredObject(d.ObjectName, d.Bucket)), objects);
            context.StartupDocumentFiles.RemoveRange(documents);

            Startup? startup = await context.Startups
                .SingleOrDefaultAsync(s => s.Id == startupId, cancellationToken);

            if (startup?.AvatarId is { } avatarId)
            {
                List<MediaFile> logo = await context.MediaFiles
                    .Where(f => f.Id == avatarId)
                    .ToListAsync(cancellationToken);

                CollectObjects(logo.Select(f => new StoredObject(f.ObjectName, f.Bucket)), objects);
                context.MediaFiles.RemoveRange(logo);
            }

            List<Message> messages = await context.Messages
                .Where(m => (m.SenderType == ChatParticipantType.Startup && m.SenderId == startupId)
                         || (m.ReceiverType == ChatParticipantType.Startup && m.ReceiverId == startupId))
                .ToListAsync(cancellationToken);

            List<Guid> messageIds = messages.ConvertAll(m => m.Id);

            List<ChatFile> chatFiles = await context.ChatFiles
                .Where(f => f.MessageId != null && messageIds.Contains(f.MessageId.Value))
                .ToListAsync(cancellationToken);

            CollectObjects(chatFiles.Select(f => new StoredObject(f.ObjectName, f.Bucket)), objects);
            context.ChatFiles.RemoveRange(chatFiles);
            context.Messages.RemoveRange(messages);

            List<Guid> dealIds = await context.InvestmentDeals
                .Where(d => d.StartupId == startupId)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            await EraseDealDocumentsAsync(dealIds, objects, cancellationToken);

            await RemoveWhereAsync(context.InvestmentDeals, d => d.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.InvestmentApplications, a => a.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.ExpertCollaborationRequests, r => r.StartupId == startupId, cancellationToken);

            await RemoveWhereAsync(context.StartupMembers, m => m.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupFollowers, f => f.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupInvestors, i => i.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupEquityHolders, h => h.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupProducts, p => p.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupMetrics, m => m.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupRoadmapItems, r => r.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupCompetitors, c => c.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupCommunityDocuments, d => d.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupCommunityStandards, s => s.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.StartupValuationSnapshots, s => s.StartupId == startupId, cancellationToken);
            await RemoveWhereAsync(context.InviteTokens, t => t.StartupId == startupId, cancellationToken);

            if (startup is not null)
            {
                context.Startups.Remove(startup);
            }
        }

        private async Task EraseDealDocumentsAsync(
            List<Guid> dealIds,
            List<StoredObject> objects,
            CancellationToken cancellationToken)
        {
            if (dealIds.Count == 0)
            {
                return;
            }

            List<DealDocument> documents = await context.DealDocuments
                .Where(d => dealIds.Contains(d.DealId))
                .ToListAsync(cancellationToken);

            foreach (DealDocument document in documents)
            {
                objects.Add(new StoredObject(document.TermSheetObjectKey, DealDocumentBuckets.DealDocuments));
                objects.Add(new StoredObject(document.CapTableObjectKey, DealDocumentBuckets.DealDocuments));
            }

            context.DealDocuments.RemoveRange(documents);
        }

        private async Task RemoveWhereAsync<T>(
            DbSet<T> set,
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken)
            where T : class
        {
            List<T> rows = await set.Where(predicate).ToListAsync(cancellationToken);

            if (rows.Count > 0)
            {
                set.RemoveRange(rows);
            }
        }

        private static void CollectObjects(IEnumerable<StoredObject> candidates, List<StoredObject> sink)
        {
            foreach (StoredObject candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate.ObjectName) && !string.IsNullOrWhiteSpace(candidate.Bucket))
                {
                    sink.Add(candidate);
                }
            }
        }

        private async Task DeleteObjectsAsync(List<StoredObject> objects, CancellationToken cancellationToken)
        {
            foreach (StoredObject stored in objects)
            {
                try
                {
                    await fileStorage.DeleteAsync(stored.ObjectName, stored.Bucket, cancellationToken);
                }
                catch (Exception exception)
                {
                    // The row is already gone, so nothing in the app can reach this object any more —
                    // but it still holds personal data until someone removes it from the bucket.
                    logger.LogError(
                        exception,
                        "Account erasure could not delete stored object {Bucket}/{ObjectName}. It must be removed manually.",
                        stored.Bucket, stored.ObjectName);
                }
            }
        }

        private async Task InvalidateCachesAsync(
            Guid userId,
            List<Guid> erasedStartupIds,
            CancellationToken cancellationToken)
        {
            await cacheService.RemoveAsync(CacheKeys.User(userId), cancellationToken);
            await cacheService.RemoveAsync(CacheKeys.UserOverview(userId), cancellationToken);
            await cacheService.RemoveAsync(CacheKeys.Profile(userId), cancellationToken);
            await cacheService.RemoveAsync(CacheKeys.SubscriptionActiveByUser(userId), cancellationToken);
            await cacheService.RemoveByPrefixAsync(CacheKeys.ServiceEntitlementsByUserPrefix(userId), cancellationToken);

            foreach (Guid startupId in erasedStartupIds)
            {
                await cacheService.RemoveAsync(CacheKeys.Startup(startupId), cancellationToken);
                await cacheService.RemoveAsync(CacheKeys.StartupScore(startupId), cancellationToken);
                await cacheService.RemoveAsync(CacheKeys.StartupCommunityStandards(startupId), cancellationToken);
            }
        }
    }
}
