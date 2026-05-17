namespace DevStart.Application.Abstractions.Authentication
{
    public sealed record ExternalUserInfo(
        string ProviderUserId,
        string? Email,
        bool EmailVerified,
        string? Name,
        string? AvatarUrl);
}
