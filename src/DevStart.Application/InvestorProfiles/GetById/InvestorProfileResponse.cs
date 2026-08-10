using DevStart.Domain.Investors;

namespace DevStart.Application.InvestorProfiles.GetById
{
    public sealed class InvestorProfileResponse
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public InvestorProfileType Type { get; init; }
        public string DisplayName { get; init; } = null!;
        public string? Bio { get; init; }
        public string? Website { get; init; }
        public bool IsPublic { get; init; }

        /// <summary>Аватарка для показа: логотип фонда либо личная аватарка физлица.</summary>
        public Guid? AvatarId { get; init; }

        /// <summary>
        /// Собственный логотип фонда без подстановки личной аватарки — форму редактирования надо
        /// заполнять именно им, иначе фонд «унаследует» чужое фото при первом же сохранении.
        /// </summary>
        public Guid? FundAvatarId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
