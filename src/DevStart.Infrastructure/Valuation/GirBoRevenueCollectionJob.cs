using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Valuation;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// Collects the denominator of the revenue multiple. MOEX does not publish revenue; ГИР БО does,
    /// by INN, for every Russian legal entity.
    ///
    /// Two properties of this data are not implementation defects and are handled explicitly:
    ///
    /// 1. <b>Publication lag.</b> Year N appears in ГИР БО in the middle of N+1, so the freshest revenue
    ///    trails by a year or more while the capitalisation is today's. The multiple is genuinely
    ///    "today's price over the year-before-last's revenue" — which is why the fiscal year rides on
    ///    every observation and, downstream, on the benchmark's source string. A number whose year is
    ///    unstated is unreadable.
    ///
    /// 2. <b>РСБУ vs IFRS.</b> ГИР БО reports РСБУ for one legal entity; the MOEX capitalisation is the
    ///    whole group. For a holding these differ by multiples and dividing them yields nonsense. So an
    ///    admin-entered consolidated figure on the issuer wins outright, and the observation is flagged
    ///    manual — the same human-in-the-loop shape the rest of this pipeline uses.
    ///
    /// Failure isolation matches the MOEX job: per-issuer errors are logged and skipped, and a barren
    /// run is a warning with counts rather than an exception.
    /// </summary>
    public sealed class GirBoRevenueCollectionJob(
        IApplicationDbContext context,
        GirBoClient client,
        IBenchmarkObservationStore store,
        IDateTimeProvider dateTimeProvider,
        ILogger<GirBoRevenueCollectionJob> logger)
    {
        private const string ManualOriginNote = "manual override: consolidated (IFRS) revenue entered by an admin";

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;
            DateTime quarterStart = BenchmarkQuarter.StartOf(now);

            List<BenchmarkIssuer> issuers = await context.BenchmarkIssuers
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Ticker)
                .ToListAsync(cancellationToken);

            if (issuers.Count == 0)
            {
                logger.LogInformation("ГИР БО collection skipped: no active benchmark issuers are registered.");
                return;
            }

            var rows = new List<IssuerObservation>(issuers.Count);
            int collected = 0;
            int manual = 0;
            int missing = 0;
            int failed = 0;

            foreach (BenchmarkIssuer issuer in issuers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // The override wins before a request is even made: when an admin has supplied the
                    // consolidated figure, the РСБУ number is not a fallback, it is the wrong number.
                    if (issuer.RevenueOverride is { } overridden && issuer.RevenueOverrideFiscalYear is { } overrideYear)
                    {
                        rows.Add(new IssuerObservation(
                            issuer.Id,
                            BenchmarkObservationSource.GirBo,
                            BenchmarkObservationMetric.Revenue,
                            overridden,
                            quarterStart,
                            overrideYear,
                            $"{ManualOriginNote}. {issuer.RevenueOverrideNote}".Trim()));

                        manual++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(issuer.Inn))
                    {
                        missing++;
                        logger.LogInformation(
                            "No INN and no revenue override for {Ticker}; it contributes no revenue this quarter.",
                            issuer.Ticker);
                        continue;
                    }

                    (decimal Revenue, int FiscalYear)? result =
                        await client.GetLatestRevenueAsync(issuer.Inn, cancellationToken);

                    if (result is null)
                    {
                        missing++;
                        logger.LogInformation(
                            "ГИР БО has no filed revenue for {Ticker} (INN {Inn}); left as an empty cell.",
                            issuer.Ticker, issuer.Inn);
                        continue;
                    }

                    rows.Add(new IssuerObservation(
                        issuer.Id,
                        BenchmarkObservationSource.GirBo,
                        BenchmarkObservationMetric.Revenue,
                        result.Value.Revenue,
                        quarterStart,
                        result.Value.FiscalYear,
                        OriginNote: $"ГИР БО РСБУ, ИНН {issuer.Inn}"));

                    collected++;
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
                        "ГИР БО collection failed for {Ticker}; the remaining issuers continue.",
                        issuer.Ticker);
                }
            }

            // One transaction for the run; a failed issuer never entered the list.
            await store.UpsertIssuerObservationsAsync(rows, cancellationToken);

            if (collected + manual == 0)
            {
                logger.LogWarning(
                    "ГИР БО collection for {Quarter} produced nothing: {Failed} failed, {Missing} without data, "
                        + "out of {Total} active issuer(s). Staging is unchanged.",
                    BenchmarkQuarter.Label(quarterStart), failed, missing, issuers.Count);
                return;
            }

            logger.LogInformation(
                "ГИР БО collection for {Quarter}: {Collected} collected, {Manual} from manual overrides, "
                    + "{Missing} without data, {Failed} failed, out of {Total} active issuer(s).",
                BenchmarkQuarter.Label(quarterStart), collected, manual, missing, failed, issuers.Count);
        }
    }
}
