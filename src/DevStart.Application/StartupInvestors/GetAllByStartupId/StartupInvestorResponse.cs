namespace DevStart.Application.StartupInvestors.GetAllByStartupId
{
    public sealed class StartupInvestorResponse
    {
        public Guid StartupId { get; set; }
        public Guid ProfileId { get; set; }
        public bool IsPublic { get; set; }

        /// <summary>Аватарка для показа: логотип фонда либо личная аватарка физлица.</summary>
        public Guid? AvatarId { get; set; }

        /// <summary>Инвестор — фонд: клиент рисует логотип квадратом, как у стартапов.</summary>
        public bool IsFund { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}