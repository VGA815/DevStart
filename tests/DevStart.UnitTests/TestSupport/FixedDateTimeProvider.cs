using DevStart.SharedKernel;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);
    }
}
