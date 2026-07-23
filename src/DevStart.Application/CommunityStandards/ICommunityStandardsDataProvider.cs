using DevStart.SharedKernel;

namespace DevStart.Application.CommunityStandards
{
    public interface ICommunityStandardsDataProvider
    {
        Task<Result<CommunityStandardsInputs>> GetInputsAsync(Guid startupId, CancellationToken cancellationToken);
    }
}
