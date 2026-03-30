using DevStart.SharedKernel;

namespace DevStart.Domain.StartupProducts
{
    public sealed class StartupProduct : Entity
    {
        public Guid StartupId { get; set; }
        public string? Problem { get; set; }
        public string Solution { get; set; } = null!;
        public List<string>? Stack { get; set; }
        public string? ValueProposition { get; set; }
        public string? Differentiators { get; set; }
        public StartupProduct()
        {
            
        }
        public static StartupProduct Create(
            Guid StartupId, string? Problem, string Solution, List<string>? Stack, string? ValueProposition, string? Differentiators)
            => new()
            {
                Differentiators = Differentiators,
                Problem = Problem,
                Solution = Solution,
                Stack = Stack,
                StartupId = StartupId,
                ValueProposition = ValueProposition,
            };
    }
}
