using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.AccountDeletion.GetStatus;
using DevStart.Domain.AccountDeletion;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.Application.AccountDeletion.RequestDeletion
{
    internal sealed class RequestAccountDeletionCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IOptions<AccountDeletionOptions> options,
        IEmailSender emailSender)
        : ICommandHandler<RequestAccountDeletionCommand, AccountDeletionStatusResponse>
    {
        public async Task<Result<AccountDeletionStatusResponse>> Handle(
            RequestAccountDeletionCommand command,
            CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
            {
                return Result.Failure<AccountDeletionStatusResponse>(UserErrors.NotFound(userId));
            }

            if (user.Role == UserSystemRole.Admin)
            {
                return Result.Failure<AccountDeletionStatusResponse>(
                    AccountDeletionErrors.AdminCannotSelfDelete);
            }

            if (user.HasPassword
                && (string.IsNullOrEmpty(command.Password)
                    || !passwordHasher.Verify(command.Password, user.PasswordHash!)))
            {
                return Result.Failure<AccountDeletionStatusResponse>(UserErrors.InvalidCurrentPassword);
            }

            bool alreadyPending = await context.AccountDeletionRequests
                .AnyAsync(
                    r => r.UserId == userId && r.Status == AccountDeletionRequestStatus.Pending,
                    cancellationToken);

            if (alreadyPending)
            {
                return Result.Failure<AccountDeletionStatusResponse>(AccountDeletionErrors.AlreadyRequested);
            }

            DateTime now = dateTimeProvider.UtcNow;
            AccountDeletionRequest request = AccountDeletionRequest.Create(userId, now, options.Value.Grace);

            context.AccountDeletionRequests.Add(request);
            await context.SaveChangesAsync(cancellationToken);

            // Out-of-band notice: if this request was not made by the account holder, the email is the
            // only channel that reaches them while the grace window is still open.
            await emailSender.SendAccountDeletionScheduled(user.Email, request.ScheduledFor);

            List<AffectedStartupResponse> startups = await context.Startups
                .AsNoTracking()
                .Where(s => SoleFounderStartups.IdsFor(context, userId).Contains(s.Id))
                .Select(s => new AffectedStartupResponse(s.Id, s.Name))
                .ToListAsync(cancellationToken);

            return new AccountDeletionStatusResponse(
                Pending: true,
                RequestedAt: request.RequestedAt,
                ScheduledFor: request.ScheduledFor,
                StartupsToDelete: startups);
        }
    }
}
