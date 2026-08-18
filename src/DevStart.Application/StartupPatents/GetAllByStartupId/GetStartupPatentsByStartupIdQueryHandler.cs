using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Registry;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupPatents.GetAllByStartupId
{
    internal sealed class GetStartupPatentsByStartupIdQueryHandler(
        IApplicationDbContext context,
        IPatentRegistryResolver resolver,
        ILegalEntityRegistry legalEntityRegistry)
        : IQueryHandler<GetStartupPatentsByStartupIdQuery, StartupPatentsResponse>
    {
        public async Task<Result<StartupPatentsResponse>> Handle(
            GetStartupPatentsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            var startup = await context.Startups
                .AsNoTracking()
                .Where(s => s.Id == query.StartupId)
                .Select(s => new { s.HasPatents, s.Inn })
                .FirstOrDefaultAsync(cancellationToken);

            if (startup is null)
            {
                return Result.Failure<StartupPatentsResponse>(StartupErrors.NotFound(query.StartupId));
            }

            StartupPatentResolution resolution = await resolver.ResolveAsync(query.StartupId, cancellationToken);

            LegalEntityResponse? legalEntity = null;
            if (!string.IsNullOrEmpty(startup.Inn))
            {
                LegalEntityLookup lookup = await legalEntityRegistry.LookupAsync(startup.Inn, cancellationToken);
                legalEntity = new LegalEntityResponse
                {
                    State = lookup.State,
                    Inn = lookup.Record?.Inn ?? startup.Inn,
                    Name = lookup.Record?.Name,
                    IsActive = lookup.Record?.IsActive,
                    StatusText = lookup.Record?.StatusText,
                    AsOf = lookup.Record?.AsOf,
                };
            }

            return new StartupPatentsResponse
            {
                StartupId = query.StartupId,
                HasPatentsDeclared = startup.HasPatents,
                DeclaredInn = resolution.DeclaredInn,
                LegalEntity = legalEntity,
                Records = resolution.Records
                    .Select(r => new StartupPatentResponse
                    {
                        Id = r.Id,
                        Kind = r.Kind,
                        Number = r.NumberRaw,
                        NumberNormalized = r.NumberNormalized,
                        State = r.State,
                        Ownership = r.Ownership,
                        Title = r.Title,
                        HolderName = r.HolderName,
                        HolderInn = r.HolderInn,
                        RegisteredOn = r.RegisteredOn,
                        ProtectionStatus = r.ProtectionStatus,
                        CreatedAt = r.CreatedAt,
                    })
                    .ToList(),
            };
        }
    }
}
