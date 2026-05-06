namespace DevStart.Infrastructure.Payments
{
    public sealed class YooKassaOptions
    {
        public string ShopId { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public string ApiUrl { get; set; } = "https://api.yookassa.ru";
        public string ReturnUrl { get; set; } = null!;
        public string[] AllowedIps { get; set; } = Array.Empty<string>();
        public bool VerifyWebhookIp { get; set; } = true;
    }
}
