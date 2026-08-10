using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;

namespace DevStart.Application.InvestorProfiles.Update
{
    public sealed class UpdateInvestorProfileCommand : ICommand
    {
        public InvestorProfileType Type { get; set; }

        // Stored on the shared Profile rather than on the investor profile — see ProfilePersonalDetails.
        public string DisplayName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public bool IsPublic { get; set; }

        // Логотип фонда — единственная аватарка, которая живёт на инвестор-профиле. У физлица
        // аватарка берётся из общего Profile, поэтому здесь значение игнорируется.
        public Guid? AvatarId { get; set; }

        public UpdateInvestorProfileCommand(
            InvestorProfileType type,
            string displayName,
            string? bio = null,
            string? website = null,
            bool isPublic = true,
            Guid? avatarId = null)
        {
            Type = type;
            DisplayName = displayName;
            Bio = bio;
            Website = website;
            IsPublic = isPublic;
            AvatarId = avatarId;
        }
    }
}
