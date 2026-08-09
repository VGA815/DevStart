using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Application.Configuration;
using DevStart.Application.Users.Security;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Authentication.TrustedDevices;
using DevStart.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.TestSupport
{
    /// <summary>
    /// Builds the auth services that most handler tests only need as collaborators, so their
    /// constructors don't have to be restated in a dozen test classes every time one gains a
    /// dependency.
    /// </summary>
    internal static class AuthTestKit
    {
        public static IUserSecuritySettingsProvider SecuritySettings(
            IApplicationDbContext db, IDateTimeProvider clock)
            => new UserSecuritySettingsProvider(db, clock);

        public static ITrustedDeviceService TrustedDevices(
            IApplicationDbContext db, IDateTimeProvider clock, TrustedDeviceOptions? options = null)
            => new TrustedDeviceService(
                db,
                SecuritySettings(db, clock),
                clock,
                Options.Create(options ?? new TrustedDeviceOptions()),
                NullLogger<TrustedDeviceService>.Instance);

        public static IRefreshTokenService RefreshTokens(
            IApplicationDbContext db,
            IDateTimeProvider clock,
            int lifetimeDays = 30,
            ITrustedDeviceService? trustedDevices = null)
            => new RefreshTokenService(
                db,
                trustedDevices ?? TrustedDevices(db, clock),
                SecuritySettings(db, clock),
                clock,
                Options.Create(new RefreshTokenOptions { LifetimeDays = lifetimeDays }));

        public static ITwoFactorLoginGate Gate(
            IApplicationDbContext db,
            IPendingTwoFactorStore pendingStore,
            IDateTimeProvider clock,
            ITrustedDeviceService? trustedDevices = null)
            => new TwoFactorLoginGate(
                db,
                pendingStore,
                trustedDevices ?? TrustedDevices(db, clock),
                SecuritySettings(db, clock));
    }
}
