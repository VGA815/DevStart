using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;

namespace DevStart.Application.ExpertProfiles.GetAll;

public enum ExpertSortBy { DisplayName, CreatedAt }

public sealed record GetExpertProfilesQuery(
    int PageNumber,
    int PageSize,
    ExpertSpecialization? Specialization = null,
    ExpertSortBy SortBy = ExpertSortBy.DisplayName) : IQuery<List<ExpertCatalogResponse>>;
