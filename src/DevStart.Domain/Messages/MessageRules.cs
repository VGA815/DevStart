namespace DevStart.Domain.Messages
{
    /// <summary>Size limits for a single chat message.</summary>
    public static class MessageRules
    {
        public const int MaxTextLength = 4000;

        public const int MaxAttachmentsPerKind = 10;
    }
}
