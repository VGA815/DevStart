using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.WebApi;
using System.Reflection;

namespace DevStart.ArchitectureTests
{
    public abstract class BaseTest
    {
        protected static readonly Assembly DomainAssembly = typeof(User).Assembly;
        protected static readonly Assembly ApplicationAssembly = typeof(ICommand).Assembly;
        protected static readonly Assembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;
        protected static readonly Assembly PresentationAssembly = typeof(Program).Assembly;
    }
}
