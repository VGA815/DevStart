using DevStart.SharedKernel;

namespace DevStart.Application.Scoring
{
    public static class ValuationErrors
    {
        /// <summary>
        /// No valuation method produced a usable result (empty ensemble or a zero range) — consumers
        /// must surface this instead of presenting a fabricated ₽0 valuation, cap or snapshot.
        /// </summary>
        public static readonly Error InsufficientData = Error.Problem(
            "Valuation.InsufficientData",
            "No valuation method produced a result for this startup; not enough data to value it.");
    }
}
