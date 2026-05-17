using DevStart.Domain.StartupProducts;
using Shouldly;

namespace DevStart.UnitTests.Domain.StartupProducts;

public sealed class StartupProductTests
{
    [Fact]
    public void Create_ShouldInitializeStartupProduct()
    {
        Guid startupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        List<string> stack = ["dotnet", "postgres"];

        StartupProduct product = StartupProduct.Create(
            startupId,
            "Problem",
            "Solution",
            stack,
            "Value",
            "Differentiators");

        product.StartupId.ShouldBe(startupId);
        product.Problem.ShouldBe("Problem");
        product.Solution.ShouldBe("Solution");
        product.Stack.ShouldBe(stack);
        product.ValueProposition.ShouldBe("Value");
        product.Differentiators.ShouldBe("Differentiators");
    }
}
