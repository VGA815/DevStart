using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Configuration;
using FluentEmail.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.Authentication
{
    internal sealed class EmailSender(IFluentEmail fluentEmail, IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator, IOptions<FrontendOptions> frontendOptions) : IEmailSender
    {
        private readonly IFluentEmail _fluentEmail = fluentEmail;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly LinkGenerator _linkGenerator = linkGenerator;
        private readonly FrontendOptions _frontendOptions = frontendOptions.Value;

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

        // The reset link targets the SPA reset form (not an API route), so the user lands on a page
        // where they can enter a new password. A relative path is used when no frontend base URL is
        // configured (SPA served same-origin as the API), mirroring the email-verification redirect.
        private string BuildPasswordResetLink(string token)
        {
            string baseUrl = _frontendOptions.BaseUrl.TrimEnd('/');
            return $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        }

        public async Task SendPasswordReset(string email, string token)
        {
            await _fluentEmail
                .To(email)
                .Subject("Password reset for DevStart")
                .Body(
                    $"To reset your password <a href='{BuildPasswordResetLink(token)}'>click here</a>. " +
                    "If you didn't request a password reset, you can safely ignore this email.",
                    isHtml: true)
                .SendAsync();
        }

        public async Task SendSubscriptionExpiring(string email, DateTime expiresAt)
        {
            await _fluentEmail
                .To(email)
                .Subject("Ваша подписка DevStart Pro скоро истечёт")
                .Body(
                    $"Подписка DevStart Pro истекает {expiresAt:yyyy-MM-dd}. " +
                    "Чтобы сохранить доступ к платным возможностям, продлите подписку в личном кабинете.",
                    isHtml: true)
                .SendAsync();
        }
    }
}
