using DevStart.Domain.Subscriptions;
using Shouldly;

namespace DevStart.UnitTests.Domain.Subscriptions;

public sealed class SubscriptionTests
{
    private static readonly DateTime CreatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreatePending_ShouldInitializePendingSubscription()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        Subscription subscription = Subscription.CreatePending(userId, SubscriptionPlan.Pro, CreatedAt);

        subscription.Id.ShouldNotBe(Guid.Empty);
        subscription.UserId.ShouldBe(userId);
        subscription.Plan.ShouldBe(SubscriptionPlan.Pro);
        subscription.Status.ShouldBe(SubscriptionStatus.Pending);
        subscription.StartedAt.ShouldBe(CreatedAt);
        subscription.ExpiresAt.ShouldBe(CreatedAt);
        subscription.CreatedAt.ShouldBe(CreatedAt);
        subscription.UpdatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void Activate_ShouldActivatePendingSubscriptionAndRaiseDomainEvent()
    {
        Subscription subscription = Subscription.CreatePending(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            SubscriptionPlan.Pro,
            CreatedAt);
        DateTime activatedAt = CreatedAt.AddMinutes(5);

        var result = subscription.Activate(activatedAt, durationDays: 30);

        result.IsSuccess.ShouldBeTrue();
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.StartedAt.ShouldBe(activatedAt);
        subscription.ExpiresAt.ShouldBe(activatedAt.AddDays(30));
        subscription.UpdatedAt.ShouldBe(activatedAt);
        SubscriptionActivatedDomainEvent domainEvent = subscription.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBeOfType<SubscriptionActivatedDomainEvent>();
        domainEvent.SubscriptionId.ShouldBe(subscription.Id);
        domainEvent.UserId.ShouldBe(subscription.UserId);
        domainEvent.Plan.ShouldBe(subscription.Plan);
        domainEvent.ExpiresAt.ShouldBe(subscription.ExpiresAt);
    }

    [Fact]
    public void Activate_ShouldBeIdempotent_WhenAlreadyActive()
    {
        Subscription subscription = Subscription.CreatePending(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            SubscriptionPlan.Pro,
            CreatedAt);
        DateTime activatedAt = CreatedAt.AddMinutes(5);
        subscription.Activate(activatedAt, durationDays: 30);

        var result = subscription.Activate(activatedAt.AddDays(1), durationDays: 30);

        result.IsSuccess.ShouldBeTrue();
        subscription.StartedAt.ShouldBe(activatedAt);
        subscription.ExpiresAt.ShouldBe(activatedAt.AddDays(30));
        subscription.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Activate_ShouldFail_WhenSubscriptionIsCancelled()
    {
        Subscription subscription = Subscription.CreatePending(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            SubscriptionPlan.Pro,
            CreatedAt);
        subscription.MarkCancelled(CreatedAt.AddMinutes(1));

        var result = subscription.Activate(CreatedAt.AddMinutes(2), durationDays: 30);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SubscriptionErrors.WrongStatusForActivation);
        subscription.Status.ShouldBe(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public void IsActiveAt_ShouldRespectStatusAndExpiration()
    {
        Subscription subscription = Subscription.CreatePending(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            SubscriptionPlan.Pro,
            CreatedAt);
        subscription.Activate(CreatedAt, durationDays: 30);

        subscription.IsActiveAt(CreatedAt.AddDays(29)).ShouldBeTrue();
        subscription.IsActiveAt(CreatedAt.AddDays(30)).ShouldBeFalse();
        subscription.MarkCancelled(CreatedAt.AddDays(1));
        subscription.IsActiveAt(CreatedAt.AddDays(2)).ShouldBeFalse();
    }
}
