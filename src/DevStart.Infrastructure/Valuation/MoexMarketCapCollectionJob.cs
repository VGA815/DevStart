using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Valuation;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// Collects the numerator of the revenue multiple: the market capitalisation of every active
    /// comparable, stamped with the quarter it describes. Quarterly — the derived benchmark is a
    /// quarterly figure, so a daily pull would only add noise and traffic.
    ///
    /// Failure isolation is the design point. One dead ticker must not cost the other thirty-nine their
    /// quarter, so a per-issuer failure is logged and skipped; MOEX being wholly unreachable ends the
    /// run as a warning with counts rather than an exception. Partial staging is deliberately allowed,
    /// because the derivation reports how many comparables a number rests on — a thin quarter is
    /// visible downstream instead of silently degrading a number that looks the same as a full one.
    ///
    /// The method never throws for these cases, which also keeps <see cref="BackgroundJobs.JobFailureAlertFilter"/>
    /// quiet: an alert that fires on a routine partial harvest is an alert nobody reads.
    /// </summary>
    public sealed class MoexMarketCapCollectionJob(
        IApplicationDbContext context,
        MoexIssClient client,
        IBenchmarkObservationStore store,
        IDateTimeProvider dateTimeProvider,
        ILogger<MoexMarketCapCollectionJob> logger)
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;
            DateTime quarterStart = BenchmarkQuarter.StartOf(now);

            // The ticker list is data, never code: a new listing is a row an admin adds, not a release.
            List<(Guid Id, string Ticker)> issuers = await context.BenchmarkIssuers
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Ticker)
                .Select(i => new ValueTuple<Guid, string>(i.Id, i.Ticker))
                .ToListAsync(cancellationToken);

            if (issuers.Count == 0)
            {
                logger.LogInformation("MOEX collection skipped: no active benchmark issuers are registered.");
                return;
            }

            var collectedRows = new List<IssuerObservation>(issuers.Count);
            int missing = 0;
            int failed = 0;

            foreach ((Guid id, string ticker) in issuers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    decimal? marketCap = await client.GetMarketCapAsync(ticker, cancellationToken);

                    if (marketCap is not > 0m)
                    {
                        missing++;
                        logger.LogWarning(
                            "MOEX returned no capitalisation for {Ticker}; skipped for {Quarter}.",
                            ticker, BenchmarkQuarter.Label(quarterStart));
                        continue;
                    }

                    collectedRows.Add(new IssuerObservation(
                        id,
                        BenchmarkObservationSource.Moex,
                        BenchmarkObservationMetric.MarketCap,
                        marketCap.Value,
                        quarterStart,
                        FiscalYear: null,
                        OriginNote: null));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failed++;
                    logger.LogWarning(
                        exception,
                        "MOEX collection failed for {Ticker}; the remaining issuers continue.",
                        ticker);
                }
            }

            int collected = collectedRows.Count;

            // One transaction for the run. A failed issuer never made it into the list, so isolation is
            // unaffected; what this buys is one round trip instead of one per comparable.
            await store.UpsertIssuerObservationsAsync(collectedRows, cancellationToken);

            if (collected == 0)
            {
                logger.LogWarning(
                    "MOEX collection for {Quarter} produced nothing: {Failed} failed, {Missing} without a figure, "
                        + "out of {Total} active issuer(s). Staging is unchanged.",
                    BenchmarkQuarter.Label(quarterStart), failed, missing, issuers.Count);
                return;
            }

            logger.LogInformation(
                "MOEX collection for {Quarter}: {Collected} collected, {Missing} without a figure, {Failed} failed, "
                    + "out of {Total} active issuer(s).",
                BenchmarkQuarter.Label(quarterStart), collected, missing, failed, issuers.Count);
        }
    }
}
