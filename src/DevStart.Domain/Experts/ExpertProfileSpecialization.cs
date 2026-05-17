namespace DevStart.Domain.Experts
{
    public sealed class ExpertProfileSpecialization
    {
        public Guid ExpertProfileId { get; set; }
        public ExpertSpecialization Specialization { get; set; }

        public ExpertProfileSpecialization()
        {
        }

        public static ExpertProfileSpecialization Create(Guid expertProfileId, ExpertSpecialization specialization)
            => new()
            {
                ExpertProfileId = expertProfileId,
                Specialization = specialization
            };
    }
}
