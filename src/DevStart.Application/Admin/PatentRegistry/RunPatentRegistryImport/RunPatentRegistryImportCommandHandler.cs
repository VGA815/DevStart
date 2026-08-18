using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.BackgroundJobs;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.SharedKernel;

namespace DevStart.Application.Admin.PatentRegistry.RunPatentRegistryImport
{
    internal sealed class RunPatentRegistryImportCommandHandler(
        IApplicationDbContext context,
        IBackgroundJobScheduler scheduler,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<RunPatentRegistryImportCommand>
    {
        public async Task<Result> Handle(
            RunPatentRegistryImportCommand command,
            CancellationToken cancellationToken)
        {
            scheduler.EnqueuePatentRegistryImport();

            DateTime now = dateTimeProvider.UtcNow;
            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.RunPatentRegistryImport,
                AdminTargetType.PatentRegistryDataset,
                Guid.Empty,
                "Поставлена в очередь загрузка реестра Роспатента.",
                now));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
