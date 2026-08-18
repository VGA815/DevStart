using System.Diagnostics;
using DevStart.Application.Abstractions.PatentRegistry;
using DevStart.Application.PatentRegistry;
using DevStart.Domain.StartupPatents;
using DevStart.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.PatentRegistry
{
    /// <summary>
    /// Quarterly refresh of the local register copy, plus an admin-triggered run. Same rhythm and same
    /// failure isolation as the benchmark collectors: one register that fails to download must not cost
    /// the others their quarter, and a failed run leaves the previous rows serving reads untouched.
    ///
    /// The method never throws for those cases, which keeps <see cref="BackgroundJobs.JobFailureAlertFilter"/>
    /// quiet — an alert that fires on a routine partial refresh is an alert nobody reads.
    /// </summary>
    public sealed class PatentRegistryImportJob(
        RospatentDumpClient client,
        IPatentRegistryStore store,
        IDateTimeProvider dateTimeProvider,
        IOptions<RospatentOptions> options,
        ILogger<PatentRegistryImportJob> logger)
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            Dictionary<string, string> configured = options.Value.DatasetUrls;

            if (configured.Count == 0)
            {
                logger.LogInformation(
                    "Rospatent import skipped: no dataset URL is configured (Rospatent:DatasetUrls). "
                        + "Records stay uncheckable rather than showing as not found.");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            int currentYear = dateTimeProvider.UtcNow.Year;
            int loadedKinds = 0;
            int failedKinds = 0;

            foreach ((string kindName, string url) in configured)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Enum.TryParse(kindName, ignoreCase: true, out IntellectualPropertyKind kind))
                {
                    failedKinds++;
                    logger.LogWarning(
                        "Rospatent import: '{KindName}' is not a known IP kind; that dataset is skipped.",
                        kindName);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                try
                {
                    string csv = await client.DownloadCsvAsync(url, cancellationToken);

                    Result<PatentRegistryParseResult> parsed =
                        RospatentDumpParser.Parse(csv, kind, currentYear);

                    if (parsed.IsFailure)
                    {
                        failedKinds++;
                        logger.LogWarning(
                            "Rospatent import for {Kind} rejected the dump: {Error}. Previous rows are unchanged.",
                            kind, parsed.Error.Description);
                        continue;
                    }

                    PatentRegistryUpsertResult result = await store.UpsertAsync(
                        parsed.Value.Records, url, cancellationToken);

                    loadedKinds++;
                    logger.LogInformation(
                        "Rospatent import for {Kind}: {Inserted} new, {Updated} refreshed, {Skipped} unusable row(s).",
                        kind, result.Inserted, result.Updated, parsed.Value.SkippedRows);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failedKinds++;
                    logger.LogWarning(
                        exception,
                        "Rospatent import for {Kind} failed; the remaining registers continue and the "
                            + "previously loaded rows keep serving reads.",
                        kind);
                }
            }

            stopwatch.Stop();
            logger.LogInformation(
                "Rospatent import finished in {ElapsedMs} ms: {Loaded} register(s) loaded, {Failed} failed.",
                stopwatch.ElapsedMilliseconds, loadedKinds, failedKinds);
        }
    }
}
