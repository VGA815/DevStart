using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;

namespace DevStart.Application.InvestorProfiles.GetAll;

public enum InvestorSortBy { DisplayName, CreatedAt }

public sealed record GetInvestorProfilesQuery(
    int PageNumber,
    int PageSize,
    InvestorProfileType? Type = null,
    InvestorSortBy SortBy = InvestorSortBy.DisplayName) : IQuery<List<InvestorCatalogResponse>>;
