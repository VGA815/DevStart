using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.ExternalLogins;

namespace DevStart.Infrastructure.Authentication.OAuth
{
    internal sealed class ExternalAuthProviderFactory : IExternalAuthProviderFactory
    {
        private readonly IReadOnlyDictionary<ExternalLoginProvider, IExternalAuthProvider> _providers;

        public ExternalAuthProviderFactory(IEnumerable<IExternalAuthProvider> providers)
        {
            _providers = providers.ToDictionary(p => p.Provider);
        }

        public IExternalAuthProvider Get(ExternalLoginProvider provider)
        {
            if (!_providers.TryGetValue(provider, out IExternalAuthProvider? impl))
            {
                throw new InvalidOperationException(
                    $"External auth provider '{provider}' is not registered");
            }
            return impl;
        }
    }
}
