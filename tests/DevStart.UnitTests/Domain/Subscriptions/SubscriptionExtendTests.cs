using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Shouldly;

namespace DevStart.UnitTests.Domain.Subscriptions;

public sealed class SubscriptionExtendTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Extend_Active_AddsDays_AndResetsReminder()
    {
        Subscription sub = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, Now);
        sub.Activate(Now, 30);
        sub.MarkRenewalReminderSent(Now);

        Result result = sub.Extend(10, Now);

        result.IsSuccess.ShouldBeTrue();
        sub.ExpiresAt.ShouldBe(Now.AddDays(40));
        sub.RenewalReminderSentAt.ShouldBeNull();
    }

    [Fact]
    public void Extend_NonActive_Fails()
    {
        Subscription sub = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, Now);

        Result result = sub.Extend(10, Now);

        result.Error.ShouldBe(SubscriptionErrors.CannotExtend);
    }

    [Fact]
    public void Extend_WhenExpiresAtInPast_MeasuresFromNow()
    {
        Subscription sub = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, Now.AddDays(-40));
        sub.Activate(Now.AddDays(-40), 30); // ExpiresAt = Now-10, still flagged Active
        sub.ExpiresAt.ShouldBeLessThan(Now);

        Result result = sub.Extend(10, Now);

        result.IsSuccess.ShouldBeTrue();
        sub.ExpiresAt.ShouldBe(Now.AddDays(10));
    }

    [Fact]
    public void Extend_NonPositiveDays_Fails()
    {
        Subscription sub = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, Now);
        sub.Activate(Now, 30);

        sub.Extend(0, Now).Error.ShouldBe(SubscriptionErrors.CannotExtend);
    }

    [Fact]
    public void CreatePending_DefaultsToPurchaseSource()
    {
        Subscription sub = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, Now);
        sub.Source.ShouldBe(SubscriptionSource.Purchase);

        Subscription granted = Subscription.CreatePending(
            Guid.NewGuid(), SubscriptionPlan.Pro, Now, SubscriptionSource.AdminGrant);
        granted.Source.ShouldBe(SubscriptionSource.AdminGrant);
    }
}
