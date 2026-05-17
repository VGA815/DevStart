using DevStart.SharedKernel;

namespace DevStart.Domain.Experts
{
    public static class ExpertExperienceErrors
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "ExpertExperiences.NotFound",
            $"The expert experience with id = '{id}' was not found.");

        public static readonly Error Unauthorized = Error.Problem(
            "ExpertExperiences.Unauthorized",
            "You are not allowed to perform this action on this expert experience.");

        public static readonly Error ExpertProfileNotFound = Error.NotFound(
            "ExpertExperiences.ExpertProfileNotFound",
            "The expert profile for this experience was not found.");
    }
}
