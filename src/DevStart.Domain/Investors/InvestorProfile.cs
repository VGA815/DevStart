using DevStart.Domain.Profiles;
using DevStart.SharedKernel;

namespace DevStart.Domain.Investors
{
    public sealed class InvestorProfile : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public InvestorProfileType Type { get; set; }

        /// <summary>
        /// Логотип фонда. Отдельная сущность от личной аватарки владельца аккаунта
        /// (<see cref="Profile.AvatarId"/>): один и тот же человек может быть экспертом «от себя»
        /// и инвестором «от фонда». Заполняется только для <see cref="InvestorProfileType.Fund"/>
        /// и очищается при переключении типа на <see cref="InvestorProfileType.Individual"/>.
        /// </summary>
        public Guid? AvatarId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Personal data (name, bio, website, visibility) lives on the shared Profile, referenced by UserId.
        // InvestorProfile only carries investor-specific data (Type, fund logo).
        public Profile Profile { get; set; } = null!;

        public InvestorProfile()
        {
        }

        public static InvestorProfile Create(
            Guid userId,
            InvestorProfileType type,
            DateTime createdAt,
            Guid? avatarId = null)
            => new()
            {
                Id = userId,
                UserId = userId,
                Type = type,
                AvatarId = AvatarFor(type, avatarId),
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Update(
            InvestorProfileType type,
            DateTime updatedAt,
            Guid? avatarId = null)
        {
            Type = type;
            AvatarId = AvatarFor(type, avatarId);
            UpdatedAt = updatedAt;
        }

        // Логотип есть только у фонда: при смене типа на физлицо он снимается, чтобы в каталоге
        // не осталось привязки к сущности, которой больше нет.
        private static Guid? AvatarFor(InvestorProfileType type, Guid? avatarId)
            => type == InvestorProfileType.Fund ? avatarId : null;
    }
}
