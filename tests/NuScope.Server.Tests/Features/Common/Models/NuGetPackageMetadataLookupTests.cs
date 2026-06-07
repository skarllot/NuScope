using Raiqub.NuSpec.Features.Common.Models;
using Xunit;

namespace Raiqub.NuSpec.Tests.Features.Common.Models;

public sealed class NuGetPackageMetadataLookupTests
{
    [Fact]
    public void FoundSetsOnlyMetadata()
    {
        var metadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "1.0.0" };

        var result = NuGetPackageMetadataLookup.Found(metadata);

        Assert.Same(metadata, result.Metadata);
        Assert.Null(result.Problem);
    }

    [Fact]
    public void NotFoundSetsOnlyProblem()
    {
        var result = NuGetPackageMetadataLookup.NotFound("Package was not found.");

        Assert.Null(result.Metadata);
        Assert.NotNull(result.Problem);
    }

    [Fact]
    public void FromProblemSetsOnlyProblem()
    {
        var problem = NuGetProblemDetailsResult.Forbidden("Access denied.");

        var result = NuGetPackageMetadataLookup.FromProblem(problem);

        Assert.Null(result.Metadata);
        Assert.Same(problem, result.Problem);
    }
}
