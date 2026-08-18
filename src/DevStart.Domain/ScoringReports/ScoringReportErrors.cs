using DevStart.SharedKernel;

namespace DevStart.Domain.ScoringReports
{
    public static class ScoringReportErrors
    {
        public static readonly Error StorageUnavailable = Error.ServiceUnavailable(
            "ScoringReports.StorageUnavailable",
            "The file storage service is temporarily unavailable. Please try again later.");
    }
}
