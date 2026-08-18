using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups;
using DevStart.Domain.PatentRegistry;
using DevStart.Domain.StartupPatents;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupPatents.GetSuggestions
{
    internal sealed class GetStartupPatentSuggestionsQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorizationService)
        : IQueryHandler<GetStartupPatentSuggestionsQuery, StartupPatentSuggestionsResponse>
    {
        public async Task<Result<StartupPatentSuggestionsResponse>> Handle(
            GetStartupPatentSuggestionsQuery query, CancellationToken cancellationToken)
        {
            var startup = await context.Startups
                .AsNoTracking()
                .Where(s => s.Id == query.StartupId)
                .Select(s => new { s.Id, s.Inn })
                .FirstOrDefaultAsync(cancellationToken);

            if (startup is null)
            {
                return Result.Failure<StartupPatentSuggestionsResponse>(StartupErrors.NotFound(query.StartupId));
            }

            // Member-only: this is a filling aid for the people who edit the card, not a public
            // "everything this company owns" listing.
            if (!await authorizationService.IsFounderOrAdminAsync(
                    userContext.UserId, query.StartupId, cancellationToken))
            {
                return Result.Failure<StartupPatentSuggestionsResponse>(UserErrors.Unauthorized());
            }

            if (string.IsNullOrEmpty(startup.Inn))
            {
                return new StartupPatentSuggestionsResponse { DeclaredInn = null };
            }

            List<PatentRegistryEntry> entries = await context.PatentRegistryEntries
                .AsNoTracking()
                .Where(e => e.HolderInn == startup.Inn)
                .OrderByDescending(e => e.RegisteredOn)
                .Take(StartupPatentSuggestionsResponse.MaxSuggestions * 2)
                .ToListAsync(cancellationToken);

            if (entries.Count == 0)
            {
                return new StartupPatentSuggestionsResponse { DeclaredInn = startup.Inn };
            }

            var existing = (await context.StartupPatents
                    .AsNoTracking()
                    .Where(p => p.StartupId == query.StartupId)
                    .Select(p => new { p.Kind, p.NumberNormalized })
                    .ToListAsync(cancellationToken))
                .Select(p => (p.Kind, p.NumberNormalized))
                .ToHashSet();

            List<StartupPatentSuggestion> suggestions = entries
                .Where(e => !existing.Contains((e.Kind, e.NumberNormalized)))
                .Take(StartupPatentSuggestionsResponse.MaxSuggestions)
                .Select(e => new StartupPatentSuggestion
                {
                    Kind = e.Kind,
                    Number = e.NumberNormalized,
                    Title = e.Title,
                    HolderName = e.HolderName,
                    RegisteredOn = e.RegisteredOn,
                    Status = e.Status,
                })
                .ToList();

            return new StartupPatentSuggestionsResponse
            {
                DeclaredInn = startup.Inn,
                Suggestions = suggestions,
            };
        }
    }
}
