using System.Text;
using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Valuation;
using DevStart.Application.Scoring.Benchmarks;
using DevStart.Domain.Admin;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.UploadDamodaranDataset
{
    internal sealed class UploadDamodaranDatasetCommandHandler(
        IApplicationDbContext context,
        IFileStorage fileStorage,
        IBenchmarkObservationStore observationStore,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UploadDamodaranDatasetCommand, UploadDamodaranDatasetResponse>
    {
        internal const string Bucket = "benchmark-datasets";

        public async Task<Result<UploadDamodaranDatasetResponse>> Handle(
            UploadDamodaranDatasetCommand command,
            CancellationToken cancellationToken)
        {
            // The validator has already rejected anything over MaxLengthBytes, so the capacity hint is
            // an upper bound the request is known to fit — and the buffer is read twice (parse, then
            // upload) rather than copied into a second array.
            using var buffer = new MemoryStream(capacity: (int)Math.Min(command.Length, 64 * 1024));
            await command.Content.CopyToAsync(buffer, cancellationToken);

            buffer.Position = 0;
            string text;
            using (var reader = new StreamReader(buffer, Encoding.UTF8, leaveOpen: true))
            {
                text = await reader.ReadToEndAsync(cancellationToken);
            }

            // Parse before storing anything. A file whose layout we cannot read is not provenance for
            // anything, so it earns neither an object in MinIO nor a row in staging.
            Result<List<DamodaranBucketObservation>> parsed = DamodaranDatasetParser.Parse(text);
            if (parsed.IsFailure)
            {
                return Result.Failure<UploadDamodaranDatasetResponse>(parsed.Error);
            }

            DateTime now = dateTimeProvider.UtcNow;
            string region = command.DatasetRegion.Trim();

            // The dataset year is in the key, so the originals of successive releases sit side by side
            // and the one a given benchmark was derived from stays findable.
            string objectKey = $"damodaran/{command.DatasetYear}/"
                + $"{now:yyyyMMddHHmmss}-{SanitizeFileName(command.FileName)}";

            buffer.Position = 0;
            await fileStorage.UploadAsync(
                objectKey,
                buffer,
                Bucket,
                string.IsNullOrWhiteSpace(command.ContentType) ? "text/csv" : command.ContentType,
                cancellationToken);

            // Storage first, then staging: an original without observations is a harmless orphan, while
            // observations without their original are a number nobody can trace back.
            await observationStore.ReplaceDamodaranYearAsync(
                command.DatasetYear, region, parsed.Value, cancellationToken);

            HashSet<string> mapped = (await context.BenchmarkIndustryMappings
                    .AsNoTracking()
                    .Where(m => m.SourceKind == BenchmarkMappingSourceKind.Damodaran)
                    .Select(m => m.ExternalKey)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int unmapped = parsed.Value.Count(b => !mapped.Contains(b.ExternalKey));

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.UploadDamodaranDataset,
                AdminTargetType.BenchmarkDataset,
                // An import targets a dataset, not a row — there is no entity id to point at. The
                // target type plus the reason line carry everything the audit needs.
                Guid.Empty,
                $"Imported Damodaran {command.DatasetYear} ({region}): {parsed.Value.Count} bucket(s), "
                    + $"{unmapped} unmapped. Original at {Bucket}/{objectKey}",
                now));

            await context.SaveChangesAsync(cancellationToken);

            return new UploadDamodaranDatasetResponse
            {
                BucketsImported = parsed.Value.Count,
                UnmappedBuckets = unmapped,
                ObjectKey = objectKey,
            };
        }

        private static string SanitizeFileName(string fileName)
        {
            var builder = new StringBuilder(fileName.Length);
            foreach (char c in Path.GetFileName(fileName))
            {
                builder.Append(char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_');
            }

            string cleaned = builder.ToString();
            return cleaned.Length == 0 ? "dataset.csv" : cleaned;
        }
    }
}
