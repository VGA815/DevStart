namespace DevStart.Application.Subscriptions
{
    public sealed class PlansOptions
    {
        public PlanOptions Pro { get; set; } = new();
    }

    public sealed class CheckoutOptions
    {
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public sealed class PlanOptions
    {
        public decimal Price { get; set; } = 2000m;
        public string Currency { get; set; } = "RUB";
        public int DurationDays { get; set; } = 30;
        public string Description { get; set; } = "DevStart Pro — 30 days";

    }
}
