using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Auth.RefreshToken
{
    internal sealed class RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IRefreshTokenService refreshTokenService,
        ITokenProvider tokenProvider)
        : ICommandHandler<RefreshTokenCommand, TokenPair>
    {
        public async Task<Result<TokenPair>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            Result<RotatedTokens> rotated = await refreshTokenService.RotateAsync(
                command.RefreshToken,
                command.IpAddress,
                command.UserAgent,
                cancellationToken);

            if (rotated.IsFailure)
            {
                return Result.Failure<TokenPair>(rotated.Error);
            }

            User? user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == rotated.Value.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<TokenPair>(UserErrors.NotFound(rotated.Value.UserId));
            }

            string accessToken = tokenProvider.CreateAccessToken(user);
            return new TokenPair(accessToken, rotated.Value.RawRefreshToken, tokenProvider.AccessTokenLifetimeSeconds);
        }
    }
}
