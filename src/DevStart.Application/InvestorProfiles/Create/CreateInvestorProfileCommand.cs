using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;

namespace DevStart.Application.InvestorProfiles.Create
{
    public sealed class CreateInvestorProfileCommand : ICommand<Guid>
    {
        public InvestorProfileType Type { get; set; }

        public CreateInvestorProfileCommand(InvestorProfileType type)
        {
            Type = type;
        }
    }
}
