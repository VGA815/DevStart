namespace DevStart.Application.Abstractions.Authentication
{
    public sealed record PkcePair(string Verifier, string Challenge);

    public interface IPkceGenerator
    {
        PkcePair Create();
    }
}
