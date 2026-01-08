using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMembers.GetAllByStartupId
{
    internal sealed class GetStartupMembersByStartupIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetStartupMembersByStartupIdQuery, List<StartupMemberResponse>>
    {
        public async Task<Result<List<StartupMemberResponse>>> Handle(GetStartupMembersByStartupIdQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
