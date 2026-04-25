using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;

namespace DevStart.Application.InvestorProfiles.Update
{
    public sealed class UpdateInvestorProfileCommand : ICommand
    {
        public InvestorProfileType Type { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public bool IsPublic { get; set; }

        public UpdateInvestorProfileCommand(InvestorProfileType type, string displayName, string? bio, string? website, bool isPublic)
        {
            Type = type;
            DisplayName = displayName;
            Bio = bio;
            Website = website;
            IsPublic = isPublic;
        }
    }
}
