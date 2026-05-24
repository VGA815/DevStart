namespace DevStart.Infrastructure.Payments
{
    /// <summary>
    /// Parameters for the YooKassa receipt object ("Чеки от ЮKassa"). Defaults target a
    /// self-employed (НПД, ФЗ-422) merchant: no VAT (<see cref="VatCode"/> = 1 — «без НДС»),
    /// a service line item, full payment. <see cref="TaxSystemCode"/> is intentionally null —
    /// the НПД cheque does not carry a 54-FZ taxation system code.
    /// </summary>
    public sealed class YooKassaReceiptOptions
    {
        public bool Enabled { get; set; } = true;
        public int VatCode { get; set; } = 1;
        public string PaymentSubject { get; set; } = "service";
        public string PaymentMode { get; set; } = "full_payment";
        public int? TaxSystemCode { get; set; }
    }
}
