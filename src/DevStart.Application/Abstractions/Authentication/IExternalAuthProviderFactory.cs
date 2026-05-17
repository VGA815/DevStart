using DevStart.Domain.ExternalLogins;

namespace DevStart.Application.Abstractions.Authentication
{
    public interface IExternalAuthProviderFactory
    {
        IExternalAuthProvider Get(ExternalLoginProvider provider);
    }
}
