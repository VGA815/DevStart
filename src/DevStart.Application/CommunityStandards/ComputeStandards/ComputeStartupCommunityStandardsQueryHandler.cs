using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;

namespace DevStart.Application.CommunityStandards.ComputeStandards
{
    internal sealed class ComputeStartupCommunityStandardsQueryHandler(
        ICommunityStandardsDataProvider dataProvider,
        ICommunityStandardsEvaluator evaluator,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<ComputeStartupCommunityStandardsQuery, CommunityStandardsResult>
    {
        public async Task<Result<CommunityStandardsResult>> Handle(
            ComputeStartupCommunityStandardsQuery query,
            CancellationToken cancellationToken)
        {
            Result<CommunityStandardsInputs> inputs = await dataProvider.GetInputsAsync(query.StartupId, cancellationToken);

            return inputs.IsFailure
                ? Result.Failure<CommunityStandardsResult>(inputs.Error)
                : evaluator.Evaluate(inputs.Value, dateTimeProvider.UtcNow);
        }
    }
}
