using Raiqub.NuSpec.Features.Common.Services;
using Xunit;

namespace Raiqub.NuSpec.Tests.Features.Common.Services;

public sealed class NuGetPackageVersionTests
{
    [Theory]
    [InlineData("1.2.3+build.5", "1.2.0", 1)]
    [InlineData("1.2.0-alpha", "1.2.0", -1)]
    [InlineData("1.2", "1.2.0", 0)]
    [InlineData("1.2.0", "1.2", 0)]
    public void TryParseParsesComparableVersions(string version, string baselineVersion, int expectedCompareTo)
    {
        var parsed = NuGetPackageVersion.TryParse(version);
        var baseline = NuGetPackageVersion.TryParse(baselineVersion);

        Assert.NotNull(parsed);
        Assert.NotNull(baseline);
        Assert.Equal(expectedCompareTo, Math.Sign(parsed!.CompareTo(baseline)));
    }

    [Fact]
    public void CompareToTreatsNullAsLowerPrecedence()
    {
        var parsed = Assert.IsType<NuGetPackageVersion>(NuGetPackageVersion.TryParse("1.2.0"));

        Assert.Equal(1, Math.Sign(parsed.CompareTo(null)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1..0")]
    [InlineData("1.-1.0")]
    [InlineData("1.a.0")]
    public void TryParseReturnsNullForInvalidReleaseParts(string version)
    {
        Assert.Null(NuGetPackageVersion.TryParse(version));
    }

    [Theory]
    [InlineData("1.0.0-alpha", "1.0.0-alpha.1", -1)]
    [InlineData("1.0.0-alpha.1", "1.0.0-alpha", 1)]
    [InlineData("1.0.0-1", "1.0.0-alpha", -1)]
    [InlineData("1.0.0-alpha", "1.0.0-1", 1)]
    [InlineData("1.0.0-alpha.10", "1.0.0-alpha.2", 1)]
    [InlineData("1.0.0-ALPHA", "1.0.0-alpha", 0)]
    public void CompareToHandlesPrereleaseIdentifierOrdering(string left, string right, int expected)
    {
        var leftVersion = Assert.IsType<NuGetPackageVersion>(NuGetPackageVersion.TryParse(left));
        var rightVersion = Assert.IsType<NuGetPackageVersion>(NuGetPackageVersion.TryParse(right));

        Assert.Equal(expected, Math.Sign(leftVersion.CompareTo(rightVersion)));
    }
}
