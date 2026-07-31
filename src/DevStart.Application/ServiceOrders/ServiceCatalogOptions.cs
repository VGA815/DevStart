using DevStart.Domain.ServiceOrders;

namespace DevStart.Application.ServiceOrders
{
    /// <summary>
    /// One-time service catalog (SC-49), bound from the "Services" configuration section. Prices are
    /// fixed per service — the platform's own service, never a percentage of anyone else's payment.
    /// </summary>
    public sealed class ServiceCatalogOptions
    {
        public List<ServiceCatalogItem> Items { get; set; } = [];

        public ServiceCatalogItem? Find(ServiceType serviceType)
            => Items.FirstOrDefault(i => i.ServiceType == serviceType);
    }

    public sealed class ServiceCatalogItem
    {
        public ServiceType ServiceType { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "RUB";
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// How long the delivered access lasts, in days. 0 means the delivery is permanent (a generated
        /// document stays readable forever); a positive value bounds it (report access, featured placement).
        /// </summary>
        public int AccessDays { get; set; }
    }
}
