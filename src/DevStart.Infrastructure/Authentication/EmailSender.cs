using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Configuration;
using FluentEmail.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.Authentication
{
    internal sealed class EmailSender(IFluentEmail fluentEmail, IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator, IOptions<FrontendOptions> frontendOptions, ILogger<EmailSender> logger) : IEmailSender
    {
        private readonly IFluentEmail _fluentEmail = fluentEmail;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly LinkGenerator _linkGenerator = linkGenerator;
        private readonly FrontendOptions _frontendOptions = frontendOptions.Value;
        private readonly ILogger<EmailSender> _logger = logger;

        private string BuildVerificationLink(string token)
        {
            string? link = _linkGenerator.GetUriByName(
                _httpContextAccessor.HttpContext!,
                "VerifyEmail",
                new { token });
            return link ?? throw new InvalidOperationException("Failed to build the email verification link.");
        }
        public async Task SendVerification(string email, string token)
        {
            string link;
            try
            {
                link = BuildVerificationLink(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not build the verification link; skipping verification email to {Recipient}", email);
                return;
            }

            IFluentEmail message = _fluentEmail
                .To(email)
                .Subject("Email verification for DevStart")
                .Body($"To verify your email address <a href='{link}'>click here</a>", isHtml: true);

            await TrySendAsync(message, "email-verification", email);
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
            IFluentEmail message = _fluentEmail
                .To(email)
                .Subject("Password reset for DevStart")
                .Body(
                    $"To reset your password <a href='{BuildPasswordResetLink(token)}'>click here</a>. " +
                    "If you didn't request a password reset, you can safely ignore this email.",
                    isHtml: true);

            await TrySendAsync(message, "password-reset", email);
        }

        public async Task SendSubscriptionExpiring(string email, DateTime expiresAt)
        {
            IFluentEmail message = _fluentEmail
                .To(email)
                .Subject("Ваша подписка DevStart Pro скоро истечёт")
                .Body(
                    $"Подписка DevStart Pro истекает {expiresAt:yyyy-MM-dd}. " +
                    "Чтобы сохранить доступ к платным возможностям, продлите подписку в личном кабинете.",
                    isHtml: true);

            await TrySendAsync(message, "subscription-expiring", email);
        }

        // Email delivery is best-effort: both the verification and password-reset flows expose a resend
        // path, so a transient SMTP failure must never bubble out as a 500 (which would also break the
        // enumeration-safe contract of the forgot-password endpoint). Log and swallow.
        private async Task TrySendAsync(IFluentEmail message, string purpose, string recipient)
        {
            try
            {
                var response = await message.SendAsync();
                if (!response.Successful)
                {
                    _logger.LogError(
                        "Failed to send {Purpose} email to {Recipient}: {Errors}",
                        purpose, recipient, string.Join("; ", response.ErrorMessages));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {Purpose} email to {Recipient}", purpose, recipient);
            }
        }
    }
}
