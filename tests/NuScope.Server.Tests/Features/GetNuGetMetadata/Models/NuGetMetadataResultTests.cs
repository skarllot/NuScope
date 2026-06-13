using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.GetNuGetMetadata.Models;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetNuGetMetadata.Models;

public sealed class NuGetMetadataResultTests
{
    [Fact]
    public void FromMetadataMapsOptionalValuesWhenRepositoryIsMissing()
    {
        var metadata = new NuGetPackageMetadata
        {
            Id = "Package.Without.Repository",
            Version = "1.0.0",
            Owners = "Example Owners",
            Summary = "Package summary",
            Language = "en-US",
            Icon = "icon.png",
            RequireLicenseAcceptance = true,
            Readme = "README.md",
            Tags = ["one", "two"],
        };

        var result = NuGetMetadataResult.FromMetadata(metadata);

        Assert.Equal("Package.Without.Repository", result.Id);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("Example Owners", result.Owners);
        Assert.Equal("Package summary", result.Summary);
        Assert.Equal("en-US", result.Language);
        Assert.Equal("icon.png", result.Icon);
        Assert.True(result.RequireLicenseAcceptance);
        Assert.Equal("README.md", result.Readme);
        Assert.Equal(["one", "two"], result.Tags);
        Assert.Null(result.Repository);
        Assert.Empty(result.DependencyGroups);
    }

    [Fact]
    public void FromMetadataMapsDependencyGroups()
    {
        var metadata = new NuGetPackageMetadata
        {
            Id = "Package",
            Version = "1.0.0",
            DependencyGroups =
            [
                new NuGetDependencyGroup
                {
                    TargetFramework = "net10.0",
                    Dependencies =
                    [
                        new NuGetDependency
                        {
                            Id = "Example.Dependency",
                            Version = "[1.0.0, )",
                            Exclude = "Build",
                        },
                    ],
                },
            ],
        };

        var result = NuGetMetadataResult.FromMetadata(metadata);

        var dependencyGroup = Assert.Single(result.DependencyGroups);
        Assert.Equal("net10.0", dependencyGroup.TargetFramework);

        var dependency = Assert.Single(dependencyGroup.Dependencies);
        Assert.Equal("Example.Dependency", dependency.Id);
        Assert.Equal("[1.0.0, )", dependency.Version);
        Assert.Equal("Build", dependency.Exclude);
    }
}
