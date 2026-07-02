using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.BackgroundJobs
{
    // Emits a structured Error event when a job permanently fails. We hook OnStateApplied (not state
    // election) so we only fire once the FailedState is actually persisted — i.e. after AutomaticRetry
    // has already decided not to reschedule — avoiding false alerts on transient, retryable failures.
    // Seq alerts on: "@Level = 'Error' and JobMethod is not null".
    internal sealed class JobFailureAlertFilter(ILogger<JobFailureAlertFilter> logger) : IApplyStateFilter
    {
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.NewState is not FailedState failedState)
            {
                return;
            }

            string jobMethod = context.BackgroundJob.Job is { } job
                ? $"{job.Type.Name}.{job.Method.Name}"
                : "unknown";

            logger.LogError(
                failedState.Exception,
                "Hangfire job {JobMethod} ({JobId}) failed permanently after exhausting retries",
                jobMethod,
                context.BackgroundJob.Id);
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }
}
