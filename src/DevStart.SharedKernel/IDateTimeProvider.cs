using System;

namespace DevStart.SharedKernel
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
