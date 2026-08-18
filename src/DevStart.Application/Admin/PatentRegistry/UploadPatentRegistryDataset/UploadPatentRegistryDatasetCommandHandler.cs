using System.Text;
using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.PatentRegistry;
using DevStart.Application.PatentRegistry;
using DevStart.Domain.Admin;
using DevStart.Domain.PatentRegistry;
using DevStart.SharedKernel;

namespace DevStart.Application.Admin.PatentRegistry.UploadPatentRegistryDataset
{
    internal sealed class UploadPatentRegistryDatasetCommandHandler(
        IApplicationDbContext context,
        IPatentRegistryStore store,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UploadPatentRegistryDatasetCommand, UploadPatentRegistryDatasetResponse>
    {
        public async Task<Result<UploadPatentRegistryDatasetResponse>> Handle(
            UploadPatentRegistryDatasetCommand command,
            CancellationToken cancellationToken)
        {
            // The cap is enforced on the bytes that actually arrive, not on the length the client
            // reported: the validator can only check what it was told, and an understated
            // Content-Length would otherwise buffer an arbitrary body into memory.
            byte[] bytes;
            try
            {
                bytes = await CappedStreamReader.ReadAsync(
                    command.Content, UploadPatentRegistryDatasetCommand.MaxLengthBytes, cancellationToken);
            }
            catch (InvalidDataException)
            {
                return Result.Failure<UploadPatentRegistryDatasetResponse>(
                    PatentRegistryErrors.DatasetTooLarge(UploadPatentRegistryDatasetCommand.MaxLengthBytes));
            }

            string text;
            try
            {
                // Strict UTF-8, for the same reason the downloader is strict: a lenient decode turns
                // Cyrillic holder names into replacement characters that then look like data.
                text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                    .GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Result.Failure<UploadPatentRegistryDatasetResponse>(
                    PatentRegistryErrors.UnreadableDataset(
                        "файл не в UTF-8 (вероятно windows-1251) — сконвертируйте его перед загрузкой"));
            }

            DateTime now = dateTimeProvider.UtcNow;

            // Parse before writing anything: a file whose layout we cannot read is not provenance for
            // any row, so it earns none.
            Result<PatentRegistryParseResult> parsed =
                RospatentDumpParser.Parse(text, command.Kind, now.Year);

            if (parsed.IsFailure)
            {
                return Result.Failure<UploadPatentRegistryDatasetResponse>(parsed.Error);
            }

            PatentRegistryUpsertResult result = await store.UpsertAsync(
                parsed.Value.Records, $"upload:{command.FileName}", cancellationToken);

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.UploadPatentRegistryDataset,
                AdminTargetType.PatentRegistryDataset,
                // An import targets a register, not a row — there is no entity id to point at.
                Guid.Empty,
                $"Загружен реестр {command.Kind} из «{command.FileName}»: {result.Inserted} новых, "
                    + $"{result.Updated} обновлено, {parsed.Value.SkippedRows} строк пропущено.",
                now));

            await context.SaveChangesAsync(cancellationToken);

            return new UploadPatentRegistryDatasetResponse
            {
                Kind = command.Kind,
                RecordsParsed = parsed.Value.Records.Count,
                Inserted = result.Inserted,
                Updated = result.Updated,
                SkippedRows = parsed.Value.SkippedRows,
            };
        }
    }
}
