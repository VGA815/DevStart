using DevStart.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DevStart.UnitTests.TestSupport
{
    internal static class InMemoryDbContextFactory
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options, new NullDomainEventsDispatcher());
        }
    }
}
