using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Shouldly;

namespace DevStart.UnitTests.Domain.Subscriptions;

public sealed class SubscriptionMaintenanceTests
{
    private static readonly DateTime CreatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

    private static Subscription ActiveSubscription()
    {
        Subscription subscription = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, CreatedAt);
        subscription.Activate(CreatedAt, durationDays: 30);
        return subscription;
    }

    [Fact]
    public void MarkExpired_FromActive_TransitionsToExpired()
    {
        Subscription subscription = ActiveSubscription();

        Result result = subscription.MarkExpired(CreatedAt.AddDays(31));

        result.IsSuccess.ShouldBeTrue();
        subscription.Status.ShouldBe(SubscriptionStatus.Expired);
        subscription.UpdatedAt.ShouldBe(CreatedAt.AddDays(31));
    }

    [Theory]
    [InlineData(SubscriptionStatus.Pending)]
    [InlineData(SubscriptionStatus.Cancelled)]
    public void MarkExpired_FromNonActive_IsNoOp(SubscriptionStatus status)
    {
        Subscription subscription = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, CreatedAt);
        if (status == SubscriptionStatus.Cancelled)
        {
            subscription.MarkCancelled(CreatedAt.AddMinutes(1));
        }

        Result result = subscription.MarkExpired(CreatedAt.AddDays(31));

        result.IsSuccess.ShouldBeTrue();
        subscription.Status.ShouldBe(status);
    }

    [Fact]
    public void MarkRenewalReminderSent_SetsTimestamp()
    {
        Subscription subscription = ActiveSubscription();
        DateTime remindedAt = CreatedAt.AddDays(27);

        subscription.MarkRenewalReminderSent(remindedAt);

        subscription.RenewalReminderSentAt.ShouldBe(remindedAt);
    }

    [Fact]
    public void Activate_ClearsAnyPriorReminderFlag()
    {
        Subscription subscription = ActiveSubscription();

        subscription.RenewalReminderSentAt.ShouldBeNull();
    }
}
