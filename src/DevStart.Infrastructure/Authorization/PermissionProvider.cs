namespace DevStart.Infrastructure.Authorization
{
    internal sealed class PermissionProvider
    {
        public Task<HashSet<string>> GetForUserIdAsync(Guid userId)
        {
            HashSet<string> permissionsSet = [];

            return Task.FromResult(permissionsSet);
        }
    }
}
