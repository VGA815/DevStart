using DevStart.Application.Configuration;
using FluentValidation;

namespace DevStart.Application.Users.Security.UpdateSecuritySettings
{
    internal sealed class UpdateSecuritySettingsCommandValidator : AbstractValidator<UpdateSecuritySettingsCommand>
    {
        public UpdateSecuritySettingsCommandValidator()
        {
            RuleFor(x => x.Strictness).IsInEnum();

            // The presets are the whole vocabulary; the per-user cap is applied by the handler, which
            // is the only place that knows the caller's role.
            RuleFor(x => x.TrustDurationDays)
                .Must(TrustedDeviceOptions.Presets.Contains)
                .WithMessage($"TrustDurationDays must be one of: {string.Join(", ", TrustedDeviceOptions.Presets)}");
        }
    }
}
