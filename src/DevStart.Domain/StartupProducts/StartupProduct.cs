using DevStart.SharedKernel;

namespace DevStart.Domain.StartupProducts
{
    public sealed class StartupProduct : Entity
    {
        public Guid StartupId { get; set; }
        public string Problem { get; set; }
        public string Solution { get; set; }
        public List<string> Stack { get; set; }
        public string ValueProposition { get; set; }
        public string Differentiators { get; set; }
        public StartupProduct(
            Guid startupId, 
            string problem, 
            string solution, 
            List<string> stack, 
            string valueProposition,
            string differentiators)
        {
            StartupId = startupId;
            Problem = problem;
            Solution = solution;
            Stack = stack;
            ValueProposition = valueProposition;
            Differentiators = differentiators;
        }
    }
}
