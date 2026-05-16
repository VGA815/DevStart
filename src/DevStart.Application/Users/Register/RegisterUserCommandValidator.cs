using DevStart.Application.UserConsents;
using FluentValidation;

namespace DevStart.Application.Users.Register
{
    internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        private static readonly Domain.UserConsents.ConsentType[] AllConsentTypes =
            Enum.GetValues<Domain.UserConsents.ConsentType>();

        public RegisterUserCommandValidator()
        {
            RuleFor(c => c.Password).NotEmpty();
            RuleFor(c => c.Email).NotEmpty();
            RuleFor(c => c.Username).NotEmpty();

            RuleFor(c => c.Consents)
                .NotEmpty()
                .WithMessage("Consents list must not be empty");

            RuleFor(c => c.Consents)
                .Must(HaveAllConsentTypes)
                .When(c => c.Consents is { Count: > 0 })
                .WithMessage($"All consent types must be provided: {string.Join(", ", AllConsentTypes)}");

            RuleFor(c => c.Consents)
                .Must(HaveNoConsentTypeDuplicates)
                .When(c => c.Consents is { Count: > 0 })
                .WithMessage("Consent list must not contain duplicate types");

            // Mandatory consents must be accepted
            RuleForEach(c => c.Consents)
                .Must(item => !ConsentVersions.MandatoryTypes.Contains(item.Type) || item.Accepted)
                .WithMessage((_, item) => $"Consent '{item.Type}' is mandatory and must be accepted");
        }

        private static bool HaveAllConsentTypes(List<ConsentItem> consents)
        {
            var providedTypes = consents.Select(c => c.Type).ToHashSet();
            return AllConsentTypes.All(t => providedTypes.Contains(t));
        }

        private static bool HaveNoConsentTypeDuplicates(List<ConsentItem> consents)
        {
            return consents.Select(c => c.Type).Distinct().Count() == consents.Count;
        }
    }
}
