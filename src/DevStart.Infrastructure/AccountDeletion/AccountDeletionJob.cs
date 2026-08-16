using DevStart.Application.Abstractions.Data;
using DevStart.Application.AccountDeletion;
using DevStart.Domain.AccountDeletion;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.AccountDeletion
{
    /// <summary>
    /// Erases the accounts whose grace window has closed. Runs daily: the window is measured in days,
    /// and the promise it has to stay inside is 30 of them.
    /// </summary>
    public sealed class AccountDeletionJob(
        IServiceScopeFactory scopeFactory,
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ILogger<AccountDeletionJob> logger)
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;

            List<Guid> dueUserIds = await context.AccountDeletionRequests
                .AsNoTracking()
                .Where(r => r.Status == AccountDeletionRequestStatus.Pending && r.ScheduledFor <= now)
                .Select(r => r.UserId)
                .ToListAsync(cancellationToken);

            if (dueUserIds.Count == 0)
            {
                return;
            }

            logger.LogInformation("Account deletion job found {Count} account(s) due for erasure.", dueUserIds.Count);

            foreach (Guid userId in dueUserIds)
            {
                // A scope per account: one erasure that fails leaves its half-built change tracker
                // behind, and the next account must not inherit it.
                using IServiceScope scope = scopeFactory.CreateScope();
                IAccountEraser eraser = scope.ServiceProvider.GetRequiredService<IAccountEraser>();

                try
                {
                    Result result = await eraser.EraseAsync(userId, cancellationToken);

                    if (result.IsFailure)
                    {
                        logger.LogError(
                            "Erasing account {UserId} failed: {Error}. It stays pending and will be retried.",
                            userId, result.Error.Code);
                    }
                }
                catch (Exception exception)
                {
                    // Deliberately swallowed per account: the request stays Pending, so the next run
                    // retries it, while the remaining accounts still get erased on time today.
                    logger.LogError(exception, "Erasing account {UserId} threw. It stays pending and will be retried.", userId);
                }
            }
        }
    }
}
