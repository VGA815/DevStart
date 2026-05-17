using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;

namespace DevStart.Application.Auth.Logout
{
    internal sealed class LogoutCommandHandler(
        IRefreshTokenService refreshTokenService)
        : ICommandHandler<LogoutCommand>
    {
        public Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
        {
            return refreshTokenService.RevokeAsync(command.RefreshToken, cancellationToken);
        }
    }
}
