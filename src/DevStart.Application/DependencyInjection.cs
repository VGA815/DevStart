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
            services.AddSingleton<CommunityStandards.ICommunityStandardsEvaluator, CommunityStandards.CommunityStandardsEvaluator>();
            services.AddSingleton<IValuationCalculator, ValuationCalculator>();
            services.AddSingleton<IDealTermsValidator, DealTermsValidator>();
            services.AddSingleton<ICapTableCalculator, CapTableCalculator>();
            services.AddSingleton<StartupEquity.Vesting.IVestingCalculator, StartupEquity.Vesting.VestingCalculator>();

            services.AddScoped<Scoring.IScoringDataProvider, Scoring.ScoringDataProvider>();

            // Resolves claimed IP records against the local register copy — read-side only: it adds a
            // provenance flag and never a point (SC-64/65).
            services.AddScoped<StartupPatents.IPatentRegistryResolver, StartupPatents.PatentRegistryResolver>();
            services.AddScoped<StartupEquity.IFoundingCapTableProvider, StartupEquity.FoundingCapTableProvider>();
            services.AddScoped<UserConsents.IConsentService, UserConsents.ConsentService>();
            services.AddScoped<Startups.IStartupAuthorizationService, Startups.StartupAuthorizationService>();

            services.AddScoped<CommunityStandards.ICommunityStandardsDataProvider, CommunityStandards.CommunityStandardsDataProvider>();
            services.AddScoped<CommunityStandards.ICommunityStandardsRefresher, CommunityStandards.CommunityStandardsRefresher>();

            services.AddScoped<Auth.TwoFactor.ITwoFactorLoginGate, Auth.TwoFactor.TwoFactorLoginGate>();
            services.AddScoped<Auth.TwoFactor.ITwoFactorEnrollmentService, Auth.TwoFactor.TwoFactorEnrollmentService>();
            services.AddScoped<Auth.TwoFactor.ITwoFactorCodeVerifier, Auth.TwoFactor.TwoFactorCodeVerifier>();
            services.AddScoped<Users.Security.IUserSecuritySettingsProvider, Users.Security.UserSecuritySettingsProvider>();

            // Default trusted-device tunables; Infrastructure binds the "TrustedDevices" section on top
            // so IOptions<TrustedDeviceOptions> resolves even when the section is absent.
            services.AddOptions<Configuration.TrustedDeviceOptions>();

            // Account erasure (ст. 21 ФЗ-152). Same shape: defaults here, "AccountDeletion" section
            // bound over them by Infrastructure.
            services.AddOptions<AccountDeletion.AccountDeletionOptions>();
            services.AddScoped<AccountDeletion.IAccountEraser, AccountDeletion.AccountEraser>();

            return services;
        }
    }
}
