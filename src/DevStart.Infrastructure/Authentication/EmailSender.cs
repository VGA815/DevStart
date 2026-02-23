using DevStart.Application.Abstractions.Authentication;
using FluentEmail.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DevStart.Infrastructure.Authentication
{
    internal sealed class EmailSender(IFluentEmail fluentEmail, IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator) : IEmailSender
    {
        private readonly IFluentEmail _fluentEmail = fluentEmail;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly LinkGenerator _linkGenerator = linkGenerator;

        private string BuildVerificationLink(string token)
        {
            string? link = _linkGenerator.GetUriByName(
                _httpContextAccessor.HttpContext!,
                "VerifyEmail",
                new { token });
            return link ?? throw new NotImplementedException();
        }
        public async Task SendVerification(string email, string token)
        {
            await _fluentEmail
                .To(email)
                .Subject("Email verification for DevStart")
                .Body($"To verify your email address <a href='{BuildVerificationLink(token)}'>click here</a>", isHtml: true)
                .SendAsync();
        }
    }
}
