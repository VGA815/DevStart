using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace DevStart.Infrastructure.Authentication
{
    internal sealed class TokenProvider(IConfiguration configuration) : ITokenProvider
    {
        public int AccessTokenLifetimeSeconds =>
            int.Parse(configuration["Jwt:ExpirationInMinutes"]!) * 60;

        public string CreateAccessToken(User user, Guid? sessionId = null)
        {
            string secretKey = configuration["Jwt:Secret"]!;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            List<Claim> claims = [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ];

            if (sessionId.HasValue)
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Sid, sessionId.Value.ToString()));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(AccessTokenLifetimeSeconds),
                SigningCredentials = credentials,
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"]
            };

            var handler = new JsonWebTokenHandler();

            return handler.CreateToken(tokenDescriptor);
        }
    }
}
