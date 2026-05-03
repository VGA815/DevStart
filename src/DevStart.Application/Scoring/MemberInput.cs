using DevStart.Domain.StartupMembers;

namespace DevStart.Application.Scoring
{
    public sealed record MemberInput(
        Guid ProfileId,
        StartupRole Role,
        StartupPosition? Position,
        int? YearsOfExperience,
        bool? HasPriorExit,
        int? PreviousStartupsCount);
}
