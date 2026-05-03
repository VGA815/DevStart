namespace DevStart.Application.Abstractions.Validation
{
    /// <summary>
    /// Soft validator: returns informative warning flags rather than rejecting the input.
    /// Both sides (investor and startup) see these flags on the application/deal response.
    /// </summary>
    public interface IDealTermsValidator
    {
        IReadOnlyList<DealTermsFlag> Validate(DealTermsInput input);
    }
}
