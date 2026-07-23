namespace DevStart.Application.CommunityStandards
{
    public interface ICommunityStandardsEvaluator
    {
        CommunityStandardsResult Evaluate(CommunityStandardsInputs inputs, DateTime utcNow);
    }
}
