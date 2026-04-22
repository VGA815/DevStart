using DevStart.Application.Abstractions.Notifications;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace DevStart.Infrastructure.Notifications
{
    internal sealed class CentrifugoTokenProvider(IOptions<CentrifugoOptions> options) : ICentrifugoTokenProvider
    {
        private readonly CentrifugoOptions _options = options.Value;

        public string CreateConnectionToken(Guid userId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.TokenHmacSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
                ]),
                Expires = DateTime.UtcNow.AddMinutes(_options.TokenExpirationInMinutes),
                SigningCredentials = credentials
            };

            var handler = new JsonWebTokenHandler();
            return handler.CreateToken(tokenDescriptor);
        }
    }
}
