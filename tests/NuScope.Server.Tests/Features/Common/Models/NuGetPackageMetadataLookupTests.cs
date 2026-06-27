using Raiqub.NuScope.Features.Common.Models;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.Common.Models;

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

public sealed class NuGetPackageVersionsLookupTests
{
    [Fact]
    public void FoundSetsOnlyVersions()
    {
        string[] versions = ["2.0.0", "1.0.0"];

        var result = NuGetPackageVersionsLookup.Found(versions);

        Assert.Same(versions, result.Versions);
        Assert.Null(result.Problem);
    }

    [Fact]
    public void NotFoundSetsOnlyProblem()
    {
        var result = NuGetPackageVersionsLookup.NotFound("Package versions were not found.");

        Assert.Null(result.Versions);
        Assert.NotNull(result.Problem);
    }
}

public sealed class NuGetToolCollectionResultTests
{
    [Fact]
    public void ReadOnlyListConstructorExposesItems()
    {
        var result = new TestCollectionResult(["first", "second"]);

        Assert.Equal(2, result.Count);
        Assert.Equal("first", result[0]);
        Assert.Equal(["first", "second"], result.ToArray());
    }

    [Fact]
    public void SpanConstructorCopiesItems()
    {
        string[] items = ["first", "second"];

        var result = new TestCollectionResult(items.AsSpan());

        items[0] = "changed";
        Assert.Equal(["first", "second"], result.ToArray());
    }

    [Fact]
    public void DefaultConstructorExposesEmptyItems()
    {
        var result = new TestCollectionResult();

        Assert.Empty(result);
    }

    [Fact]
    public void NonGenericEnumeratorExposesItems()
    {
        System.Collections.IEnumerable result = new TestCollectionResult(["first", "second"]);

        Assert.Equal(["first", "second"], result.Cast<string>().ToArray());
    }

    private sealed record TestCollectionResult : NuGetToolCollectionResult<string>
    {
        public TestCollectionResult() { }

        public TestCollectionResult(IReadOnlyList<string> items)
            : base(items) { }

        public TestCollectionResult(ReadOnlySpan<string> items)
            : base(items) { }
    }
}
