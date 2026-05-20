using FluentValidation;

namespace DevStart.Application.ExpertCollaborationRequests.Reject
{
    internal sealed class RejectExpertCollaborationRequestCommandValidator : AbstractValidator<RejectExpertCollaborationRequestCommand>
    {
        public RejectExpertCollaborationRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }
}
