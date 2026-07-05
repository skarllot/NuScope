using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.ListTypes.Models;
using Raiqub.NuScope.Features.ListTypes.Services;
using Raiqub.NuScope.Features.ListTypes.Tools;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.ListTypes.Tools;

public sealed class NuGetListTypesToolTests
{
    [Fact]
    public void ListTypesReturnsCollectionResultWhenServiceFindsTypes()
    {
        var tool = new NuGetListTypesTool(
            new StubTypeListingService(
                NuGetPackageTypesLookup.Found([
                    new NuGetTypeAssemblyResult
                    {
                        Assembly = "Example.dll",
                        Exported = [],
                        Types = ["class Example.Type"],
                    },
                ])
            )
        );

        var result = tool.ListTypes("Example.Package", "1.0.0", "net8.0");

        var success = Assert.IsType<NuGetListTypesResult>(result);
        var assembly = Assert.Single(success);
        Assert.Equal("Example.dll", assembly.Assembly);
        Assert.Empty(assembly.Exported);
        Assert.Equal(["class Example.Type"], assembly.Types);
    }

    [Fact]
    public void ListTypesReturnsProblemWhenServiceFails()
    {
        var problem = NuGetProblemDetailsResult.NotFound("Package was not found.");
        var tool = new NuGetListTypesTool(new StubTypeListingService(NuGetPackageTypesLookup.FromProblem(problem)));

        var result = tool.ListTypes("Missing.Package", "1.0.0", "net8.0");

        Assert.Same(problem, result);
    }

    private sealed class StubTypeListingService(NuGetPackageTypesLookup result) : INuGetPackageTypeListingService
    {
        public NuGetPackageTypesLookup ListTypes(
            string packageName,
            string version,
            string targetFramework,
            string? filterRegex = null,
            bool includePrivate = false,
            bool includeExported = false
        )
        {
            return result;
        }
    }
}
