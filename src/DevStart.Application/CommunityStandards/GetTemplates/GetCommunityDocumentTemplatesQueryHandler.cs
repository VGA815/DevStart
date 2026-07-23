using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;

namespace DevStart.Application.CommunityStandards.GetTemplates
{
    internal sealed class GetCommunityDocumentTemplatesQueryHandler(ICommunityDocumentTemplateProvider templateProvider)
        : IQueryHandler<GetCommunityDocumentTemplatesQuery, List<CommunityDocumentTemplate>>
    {
        public Task<Result<List<CommunityDocumentTemplate>>> Handle(
            GetCommunityDocumentTemplatesQuery query,
            CancellationToken cancellationToken)
            => Task.FromResult(Result.Success(templateProvider.GetAll().ToList()));
    }
}
