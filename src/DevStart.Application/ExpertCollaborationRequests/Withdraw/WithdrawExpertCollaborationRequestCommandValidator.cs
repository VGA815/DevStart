using FluentValidation;

namespace DevStart.Application.ExpertCollaborationRequests.Withdraw
{
    internal sealed class WithdrawExpertCollaborationRequestCommandValidator : AbstractValidator<WithdrawExpertCollaborationRequestCommand>
    {
        public WithdrawExpertCollaborationRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }
}
