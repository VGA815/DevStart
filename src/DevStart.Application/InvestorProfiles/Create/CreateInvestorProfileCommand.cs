using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;

namespace DevStart.Application.InvestorProfiles.Create
{
    public sealed class CreateInvestorProfileCommand : ICommand<Guid>
    {
        public InvestorProfileType Type { get; set; }

        // Stored on the shared Profile rather than on the investor profile — see ProfilePersonalDetails.
        public string DisplayName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public bool IsPublic { get; set; }

        public CreateInvestorProfileCommand(
            InvestorProfileType type,
            string displayName,
            string? bio = null,
            string? website = null,
            bool isPublic = true)
        {
            Type = type;
            DisplayName = displayName;
            Bio = bio;
            Website = website;
            IsPublic = isPublic;
        }
    }
}
