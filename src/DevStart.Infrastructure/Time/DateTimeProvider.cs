using DevStart.SharedKernel;

namespace DevStart.Infrastructure.Time
{
    internal sealed class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
