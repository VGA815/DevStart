using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.Login
{
    public sealed class LoginUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider,
        IRefreshTokenService refreshTokenService) : ICommandHandler<LoginUserCommand, TokenPair>
    {
        public async Task<Result<TokenPair>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

            if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return Result.Failure<TokenPair>(UserErrors.NotFoundByEmail);
            }

            bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

            if (!verified)
            {
                return Result.Failure<TokenPair>(UserErrors.NotFoundByEmail);
            }

            string accessToken = tokenProvider.CreateAccessToken(user);
            IssuedRefreshToken refresh = await refreshTokenService.IssueAsync(
                user,
                command.IpAddress,
                command.UserAgent,
                cancellationToken);

            return new TokenPair(accessToken, refresh.RawToken, tokenProvider.AccessTokenLifetimeSeconds);
        }
    }
}
