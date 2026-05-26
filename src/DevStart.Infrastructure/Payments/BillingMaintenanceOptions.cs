namespace DevStart.Infrastructure.Payments
{
    /// <summary>
    /// Tuning for the recurring billing jobs. Bound from the optional "Billing" configuration
    /// section; sensible production defaults apply when absent.
    /// </summary>
    public sealed class BillingMaintenanceOptions
    {
        /// <summary>Only reconcile payments older than this (gives the webhook time to arrive first).</summary>
        public int ReconcileMinAgeMinutes { get; set; } = 10;

        /// <summary>Stop reconciling payments older than this (treated as abandoned).</summary>
        public int ReconcileMaxAgeHours { get; set; } = 72;

        /// <summary>
        /// Also re-sync captured (Succeeded) payments paid within this many hours so a missed or
        /// out-of-band <c>refund.succeeded</c> event is still reflected locally.
        /// </summary>
        public int RefundReconcileWindowHours { get; set; } = 72;

        /// <summary>How many days before expiry to send the renewal reminder.</summary>
        public int ReminderDaysBefore { get; set; } = 3;
    }
}
