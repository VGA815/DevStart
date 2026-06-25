using DevStart.Application.Abstractions.Behaviors;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Validation;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.Scoring;
using DevStart.SharedKernel;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DevStart.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
                .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

            services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
            services.Decorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandBaseHandler<>));

            services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));
            services.Decorate(typeof(IQueryHandler<,>), typeof(CachingDecorator.QueryHandler<,>));
            services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
            services.Decorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));

            services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
                .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

            // Default valuation constants; the WebApi/Infrastructure layer binds the "Valuation"
            // configuration section on top of these so IOptions<ValuationOptions> is always resolvable.
            services.AddOptions<ValuationOptions>();

            services.AddSingleton<IScoringEngine, ScoringEngine>();
            services.AddSingleton<IValuationCalculator, ValuationCalculator>();
            services.AddSingleton<IDealTermsValidator, DealTermsValidator>();
            services.AddSingleton<ICapTableCalculator, CapTableCalculator>();

            services.AddScoped<Scoring.IScoringDataProvider, Scoring.ScoringDataProvider>();
            services.AddScoped<UserConsents.IConsentService, UserConsents.ConsentService>();
            services.AddScoped<Startups.IStartupAuthorizationService, Startups.StartupAuthorizationService>();

            return services;
        }
    }
}
