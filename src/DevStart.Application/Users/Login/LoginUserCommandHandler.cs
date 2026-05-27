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
        // A precomputed, well-formed hash (64-hex hash + 32-hex salt) used only to run the verifier on
        // the user-not-found path. Running PBKDF2 anyway keeps the response time of an unknown email
        // comparable to a wrong-password attempt, so timing can't be used to enumerate accounts.
        private const string DummyPasswordHash =
            "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF-0123456789ABCDEF0123456789ABCDEF";

        public async Task<Result<TokenPair>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

            if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            {
                // Equalize timing with the real verification path below to avoid leaking whether the
                // email is registered.
                passwordHasher.Verify(command.Password, DummyPasswordHash);
                return Result.Failure<TokenPair>(UserErrors.NotFoundByEmail);
            }

            bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

            if (!verified)
            {
                return Result.Failure<TokenPair>(UserErrors.NotFoundByEmail);
            }

            if (!user.IsVerified)
            {
                return Result.Failure<TokenPair>(UserErrors.EmailNotVerified);
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
