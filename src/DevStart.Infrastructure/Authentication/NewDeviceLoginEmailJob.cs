using DevStart.Application.Abstractions.Authentication;

namespace DevStart.Infrastructure.Authentication
{
    /// <summary>
    /// Sends the "signed in from a new device" warning off the request thread.
    /// </summary>
    public sealed class NewDeviceLoginEmailJob(IEmailSender emailSender)
    {
        public Task SendAsync(string email, string? browser, string? os, string? ipAddress, DateTime occurredAtUtc)
            => emailSender.SendNewDeviceLogin(
                email, new NewDeviceLoginInfo(browser, os, ipAddress, occurredAtUtc));
    }
}
