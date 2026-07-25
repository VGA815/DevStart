using DevStart.SharedKernel;

namespace DevStart.Domain.Payments
{
    /// <summary>
    /// Raised when a payment actually transitions to <see cref="PaymentStatus.Succeeded"/> (once,
    /// on the real transition — never on idempotent replays). Carries the gross amount and paid-at
    /// instant so downstream can attribute the income to the correct calendar year (НПД limit).
    /// </summary>
    public sealed record PaymentSucceededDomainEvent(
        Guid PaymentId,
        Guid UserId,
        decimal Amount,
        DateTime PaidAt) : IDomainEvent;
}
