using System.IO.Abstractions.TestingHelpers;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.GetNuGetVersions.Models;
using Raiqub.NuScope.Features.GetNuGetVersions.Tools;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetNuGetVersions.Tools;

public sealed class GetNuGetVersionsToolTests
{
    [Fact]
    public void GetNuGetVersionsReturnsVersionsResultWhenPackageExists()
    {
        var packageRootDirectory = GetPackageRootDirectory("Newtonsoft.Json");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "12.0.1"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "13.0.3"));

        var result = new GetNuGetVersionsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetVersions("Newtonsoft.Json", minimumMajor: 13);

        var success = Assert.IsType<NuGetVersionsResult>(result);
        Assert.Equal(["13.0.3"], success.ToArray());
    }

    [Fact]
    public void GetNuGetVersionsIncludesPrereleaseVersionsWhenRequested()
    {
        var packageRootDirectory = GetPackageRootDirectory("Newtonsoft.Json");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "13.0.3-beta.1"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "13.0.3"));

        var result = new GetNuGetVersionsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetVersions("Newtonsoft.Json", minimumMajor: 13, includePreRelease: true);

        var success = Assert.IsType<NuGetVersionsResult>(result);
        Assert.Equal(["13.0.3", "13.0.3-beta.1"], success.ToArray());
    }

    [Fact]
    public void GetNuGetVersionsLimitsVersionsWhenRequested()
    {
        var packageRootDirectory = GetPackageRootDirectory("Newtonsoft.Json");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "11.0.1"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "12.0.1"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "13.0.3"));

        var result = new GetNuGetVersionsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetVersions("Newtonsoft.Json", maxItems: 2);

        var success = Assert.IsType<NuGetVersionsResult>(result);
        Assert.Equal(["13.0.3", "12.0.1"], success.ToArray());
    }

    [Fact]
    public void GetNuGetVersionsReturnsNotFoundWhenPackageIsMissing()
    {
        var fileSystem = new MockFileSystem();

        var result = new GetNuGetVersionsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetVersions("Missing.Package");

        var problem = Assert.IsType<NuGetProblemDetailsResult>(result);
        Assert.Equal(ProblemTypes.NotFound, problem.Type);
        Assert.Equal("Not Found", problem.Title);
        Assert.Equal(404, problem.Status);
        Assert.Contains("versions were not found", problem.Detail);
    }

    private static string GetPackageRootDirectory(string packageName)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages", packageName.ToLowerInvariant());
    }
}
