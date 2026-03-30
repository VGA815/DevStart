namespace DevStart.Application.StartupProducts.GetById
{
    public sealed class StartupProductResponse
    {
        public Guid StartupId { get; init; }
        public string? Problem { get; init; }
        public string Solution { get; init; } = null!;
        public List<string>? Stack { get; init; } = [];
        public string? ValueProposition { get; init; }
        public string? Differentiators { get; init; }
    }
}