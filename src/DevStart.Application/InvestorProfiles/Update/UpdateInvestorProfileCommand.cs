using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;

namespace DevStart.Application.InvestorProfiles.Update
{
    public sealed class UpdateInvestorProfileCommand : ICommand
    {
        public InvestorProfileType Type { get; set; }

        public UpdateInvestorProfileCommand(InvestorProfileType type)
        {
            Type = type;
        }
    }
}
