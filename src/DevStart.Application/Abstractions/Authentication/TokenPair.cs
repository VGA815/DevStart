namespace DevStart.Application.Abstractions.Authentication
{
    public sealed record TokenPair(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn);
}
