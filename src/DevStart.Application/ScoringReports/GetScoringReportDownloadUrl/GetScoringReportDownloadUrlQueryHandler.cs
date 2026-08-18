using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.ServiceOrders;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.Scoring;
using DevStart.Application.Startups.GetScore;
using DevStart.Domain.ScoringReports;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Startups;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DevStart.Application.ScoringReports.GetScoringReportDownloadUrl
{
    /// <summary>
    /// Hands out a short-lived link to the PDF scoring report.
    /// <para>
    /// The gate is the one <c>GetStartupScoreQueryHandler</c> already applies to the on-screen score:
    /// members of the startup always, outside viewers with active Pro, or with a paid
    /// <see cref="ServiceType.ScoringReport"/> entitlement for this startup. The paid service was
    /// always called a report and until now delivered a web page; this closes that gap without
    /// changing who may see what.
    /// </para>
    /// <para>
    /// The file is rendered on first request and kept under a key derived from the moment the score
    /// was computed. A later recomputation produces a different key rather than overwriting the
    /// earlier file, so a report someone has already been handed keeps saying what it said.
    /// </para>
    /// </summary>
    internal sealed class GetScoringReportDownloadUrlQueryHandler(
        IApplicationDbContext context,
        IQueryHandler<ComputeStartupScoreQuery, ScoreResult> computeScoreHandler,
        IUserContext userContext,
        ISubscriptionChecker subscriptionChecker,
        IServiceEntitlementChecker entitlementChecker,
        IScoringReportComposer composer,
        IScoringReportPdfRenderer renderer,
        IFileStorage fileStorage,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetScoringReportDownloadUrlQuery, ScoringReportDownloadUrlResponse>
    {
        private const int ExpirySeconds = 600; // 10 min, as for the term sheet

        public async Task<Result<ScoringReportDownloadUrlResponse>> Handle(
            GetScoringReportDownloadUrlQuery query,
            CancellationToken cancellationToken)
        {
            Startup? startup = await context.Startups
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == query.StartupId, cancellationToken);
            if (startup is null)
            {
                return Result.Failure<ScoringReportDownloadUrlResponse>(
                    StartupErrors.NotFound(query.StartupId));
            }

            Guid viewerId = userContext.UserId;
            bool isMember = await context.StartupMembers
                .AsNoTracking()
                .AnyAsync(sm => sm.StartupId == query.StartupId && sm.ProfileId == viewerId, cancellationToken);
            if (!isMember
                && !await subscriptionChecker.HasActiveProAsync(viewerId, cancellationToken)
                && !await entitlementChecker.HasAsync(
                        viewerId, ServiceType.ScoringReport, query.StartupId, cancellationToken))
            {
                return Result.Failure<ScoringReportDownloadUrlResponse>(SubscriptionErrors.ProRequired);
            }

            Result<ScoreResult> scoreResult = await computeScoreHandler.Handle(
                new ComputeStartupScoreQuery(query.StartupId), cancellationToken);
            if (scoreResult.IsFailure)
            {
                return Result.Failure<ScoringReportDownloadUrlResponse>(scoreResult.Error);
            }

            // A report is produced even when scoring found nothing usable: it then says so in words.
            // Refusing to produce one would leave a paying reader with no document at all and no
            // statement of why.
            ScoringReportModel model = composer.Compose(startup, scoreResult.Value);
            byte[] pdf = renderer.Render(model);
            string sha256 = Convert.ToHexStringLower(SHA256.HashData(pdf));
            string objectKey = ScoringReportStorage.ObjectKey(query.StartupId, model.CalculatedAt);
            string fileName = $"scoring-report-{query.StartupId}-{model.CalculatedAt:yyyy-MM-dd}.pdf";

            string url;
            try
            {
                // Upload before presigning, and unconditionally: the renderer is deterministic, so
                // re-uploading the same key writes the same bytes. That is cheaper than an existence
                // check and cannot leave a link pointing at a missing object.
                using (var stream = new MemoryStream(pdf, writable: false))
                {
                    await fileStorage.UploadAsync(
                        objectKey,
                        stream,
                        ScoringReportStorage.Bucket,
                        "application/pdf",
                        cancellationToken);
                }

                url = await fileStorage.GetPresignedUrl(
                    objectKey,
                    ScoringReportStorage.Bucket,
                    ExpirySeconds,
                    cancellationToken,
                    fileName);
            }
            catch (FileStorageException)
            {
                return Result.Failure<ScoringReportDownloadUrlResponse>(ScoringReportErrors.StorageUnavailable);
            }

            return new ScoringReportDownloadUrlResponse
            {
                StartupId = query.StartupId,
                Url = url,
                ExpiresAt = dateTimeProvider.UtcNow.AddSeconds(ExpirySeconds),
                FileName = fileName,
                Sha256 = sha256,
                CalculatedAt = model.CalculatedAt,
            };
        }
    }
}
