using DevStart.SharedKernel;

namespace DevStart.Application.AccountDeletion
{
    public interface IAccountEraser
    {
        /// <summary>
        /// Erases a user's personal data (ст. 21 ФЗ-152) and closes out their deletion request.
        /// Idempotent: erasing an account that is already gone succeeds without doing anything.
        /// </summary>
        Task<Result> EraseAsync(Guid userId, CancellationToken cancellationToken);
    }
}
