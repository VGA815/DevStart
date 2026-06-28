using DevStart.Application.Scoring;
using DevStart.Domain.Valuation;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;
using Shouldly;

namespace DevStart.ArchitectureTests.Layers
{
    /// <summary>
    /// Keeps the valuation-benchmark module on the right side of the layer boundaries: the read
    /// abstraction and the engine's value object in Application, the storage-backed provider and EF
    /// configuration in Infrastructure, the entity in Domain, and the admin contracts in Application.
    /// </summary>
    public class ValuationBenchmarkPlacementTests : BaseTest
    {
        [Fact]
        public void ProviderAbstraction_And_BenchmarkSet_LiveInApplication()
        {
            typeof(IValuationBenchmarkProvider).Assembly.ShouldBe(ApplicationAssembly);
            typeof(ValuationBenchmarkSet).Assembly.ShouldBe(ApplicationAssembly);
        }

        [Fact]
        public void BenchmarkEntity_LivesInDomain()
        {
            typeof(ValuationBenchmark).Assembly.ShouldBe(DomainAssembly);
        }

        [Fact]
        public void ProviderImplementation_LivesInInfrastructureValuationNamespace()
        {
            TestResult result = Types.InAssembly(InfrastructureAssembly)
                .That()
                .ImplementInterface(typeof(IValuationBenchmarkProvider))
                .Should()
                .ResideInNamespace("DevStart.Infrastructure.Valuation")
                .GetResult();

            result.IsSuccessful.ShouldBeTrue();
        }

        [Fact]
        public void BenchmarkEntityConfiguration_LivesInInfrastructure()
        {
            TestResult result = Types.InAssembly(InfrastructureAssembly)
                .That()
                .ImplementInterface(typeof(IEntityTypeConfiguration<ValuationBenchmark>))
                .Should()
                .ResideInNamespace("DevStart.Infrastructure.Valuation")
                .GetResult();

            result.IsSuccessful.ShouldBeTrue();
        }

        [Fact]
        public void AdminBenchmarkContracts_LiveInApplication_AndDoNotLeakIntoOtherLayers()
        {
            const string adminNamespace = "DevStart.Application.Admin.Valuation";

            Types.InAssembly(ApplicationAssembly)
                .That()
                .ResideInNamespace(adminNamespace)
                .Should()
                .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
                .GetResult()
                .IsSuccessful.ShouldBeTrue();

            // The admin benchmark contracts must not exist in Infrastructure or Presentation assemblies.
            Types.InAssembly(InfrastructureAssembly)
                .That().ResideInNamespace(adminNamespace).GetTypes().ShouldBeEmpty();
            Types.InAssembly(PresentationAssembly)
                .That().ResideInNamespace(adminNamespace).GetTypes().ShouldBeEmpty();
        }
    }
}
