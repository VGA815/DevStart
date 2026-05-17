using DevStart.Domain.Investors;

namespace DevStart.Application.InvestorProfiles.GetAll;

public sealed class InvestorCatalogResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public InvestorProfileType Type { get; init; }
    public string DisplayName { get; init; } = null!;
    public string? Bio { get; init; }
    public string? Website { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
