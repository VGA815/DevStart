using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Auth.TwoFactor.SetupLogin
{
    /// <summary>
    /// Starts mandatory 2FA enrollment during login (admins without 2FA). The caller proved the
    /// first factor already — the pending token issued by the login gate is the credential here.
    /// </summary>
    internal sealed class SetupTwoFactorLoginCommandHandler(
        IApplicationDbContext context,
        IPendingTwoFactorStore pendingStore,
        ITwoFactorEnrollmentService enrollment,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<SetupTwoFactorLoginCommand, TwoFactorLoginSetupResponse>
    {
        public async Task<Result<TwoFactorLoginSetupResponse>> Handle(
            SetupTwoFactorLoginCommand command, CancellationToken cancellationToken)
        {
            PendingTwoFactorLogin? pending = await pendingStore.GetAsync(command.PendingToken, cancellationToken);
            if (pending is null || !pending.SetupRequired)
            {
                return Result.Failure<TwoFactorLoginSetupResponse>(TwoFactorErrors.ChallengeExpired);
            }

            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == pending.UserId, cancellationToken);
            if (user is null)
            {
                await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);
                return Result.Failure<TwoFactorLoginSetupResponse>(TwoFactorErrors.ChallengeExpired);
            }
            if (user.IsCurrentlyBanned(dateTimeProvider.UtcNow))
            {
                await pendingStore.RemoveAsync(command.PendingToken, cancellationToken);
                return Result.Failure<TwoFactorLoginSetupResponse>(UserErrors.Banned);
            }

            Result<TwoFactorSetupData> setup = await enrollment.StartAsync(user, cancellationToken);
            if (setup.IsFailure)
            {
                return Result.Failure<TwoFactorLoginSetupResponse>(setup.Error);
            }

            return new TwoFactorLoginSetupResponse(setup.Value.Secret, setup.Value.OtpAuthUri, command.PendingToken);
        }
    }
}
