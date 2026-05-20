using FluentValidation;

namespace DevStart.Application.ExpertCollaborationRequests.Accept
{
    internal sealed class AcceptExpertCollaborationRequestCommandValidator : AbstractValidator<AcceptExpertCollaborationRequestCommand>
    {
        public AcceptExpertCollaborationRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }
}
