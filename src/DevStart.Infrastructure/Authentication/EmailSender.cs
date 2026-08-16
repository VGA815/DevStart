using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Configuration;
using FluentEmail.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

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

        // Every interpolated value is HTML-encoded. Today none of them can carry markup — browser/OS
        // come from a closed set of parser literals and the IP is a formatted IPAddress — but this is
        // the one email built from request-derived data, and putting the raw User-Agent in here later
        // would otherwise turn it into an injection point silently.
        public async Task SendNewDeviceLogin(string email, NewDeviceLoginInfo info)
        {
            string settingsUrl = Encode($"{_frontendOptions.BaseUrl.TrimEnd('/')}/dashboard/settings");
            string browser = Encode(info.Browser ?? "неизвестный браузер");
            string os = Encode(info.Os ?? "неизвестная система");
            string where = Encode(string.IsNullOrWhiteSpace(info.IpAddress) ? "неизвестен" : info.IpAddress);
            string occurredAt = Encode(info.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm"));

            IFluentEmail message = _fluentEmail
                .To(email)
                .Subject("Новый вход в аккаунт DevStart")
                .Body(
                    $"В ваш аккаунт DevStart вошли с устройства, которым вы давно не пользовались.<br><br>" +
                    $"Браузер: {browser}<br>" +
                    $"Система: {os}<br>" +
                    $"IP-адрес: {where}<br>" +
                    $"Время (UTC): {occurredAt}<br><br>" +
                    "Если это были вы — ничего делать не нужно. Если нет — смените пароль и завершите " +
                    $"чужие сессии в <a href='{settingsUrl}'>настройках безопасности</a>.",
                    isHtml: true);

            await TrySendAsync(message, "new-device-login", email);
        }

        public async Task SendAccountDeletionScheduled(string email, DateTime scheduledFor)
        {
            string settingsUrl = Encode($"{_frontendOptions.BaseUrl.TrimEnd('/')}/dashboard/settings");
            string when = Encode(scheduledFor.ToString("yyyy-MM-dd"));

            IFluentEmail message = _fluentEmail
                .To(email)
                .Subject("Запрос на удаление аккаунта DevStart")
                .Body(
                    $"Мы получили запрос на удаление вашего аккаунта DevStart.<br><br>" +
                    $"Аккаунт и связанные с ним персональные данные будут удалены {when} (UTC). " +
                    "До этой даты аккаунт продолжает работать, а запрос можно отменить.<br><br>" +
                    $"Если вы передумали или запрос сделали не вы — отмените удаление в " +
                    $"<a href='{settingsUrl}'>настройках безопасности</a> и смените пароль.",
                    isHtml: true);

            await TrySendAsync(message, "account-deletion-scheduled", email);
        }

        private static string Encode(string value) => HtmlEncoder.Default.Encode(value);

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
