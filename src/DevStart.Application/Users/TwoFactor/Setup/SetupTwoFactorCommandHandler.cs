using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.TwoFactor.Setup
{
    internal sealed class SetupTwoFactorCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ITwoFactorEnrollmentService enrollment) : ICommandHandler<SetupTwoFactorCommand, TwoFactorSetupData>
    {
        public async Task<Result<TwoFactorSetupData>> Handle(
            SetupTwoFactorCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<TwoFactorSetupData>(UserErrors.NotFound(userContext.UserId));
            }

            return await enrollment.StartAsync(user, cancellationToken);
        }
    }
}
