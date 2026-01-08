namespace DevStart.Application.StartupProducts.GetById
{
    public sealed class StartupProductResponse
    {
        public Guid StartupId { get; init; }
        public string Problem { get; init; } = null!;
        public string Solution { get; init; } = null!;
        public List<string> Stack { get; init; } = [];
        public string ValueProposition { get; init; } = null!;
        public string Differentiators { get; init; } = null!;
    }
}