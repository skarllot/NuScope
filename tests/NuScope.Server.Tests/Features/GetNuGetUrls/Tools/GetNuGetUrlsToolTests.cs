using System.IO.Abstractions.TestingHelpers;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.GetNuGetUrls.Models;
using Raiqub.NuScope.Features.GetNuGetUrls.Tools;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetNuGetUrls.Tools;

public sealed class GetNuGetUrlsToolTests
{
    [Fact]
    public void GetNuGetUrlsReturnsProjectRepositoryAndOtherUrlsWhenPackageExists()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Urls", "1.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.with.urls.nuspec");
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                [nuspecPath] = new MockFileData(
                    """
                    <?xml version="1.0" encoding="utf-8"?>
                    <package>
                      <metadata>
                        <id>Package.With.Urls</id>
                        <version>1.0.0</version>
                        <projectUrl>https://example.com/project</projectUrl>
                        <iconUrl>https://example.com/icon.png</iconUrl>
                        <licenseUrl>https://example.com/license</licenseUrl>
                        <description>Docs are at https://example.com/docs.</description>
                        <releaseNotes>See https://example.com/releases/1.0.0, and https://example.com/icon.png</releaseNotes>
                        <repository type="git" url="https://github.com/example/package" />
                      </metadata>
                    </package>
                    """
                ),
            }
        );

        fileSystem.AddDirectory(packageDirectory);

        var result = new GetNuGetUrlsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetUrls("Package.With.Urls", "1.0.0");

        var success = Assert.IsType<NuGetPackageUrlsResult>(result);
        Assert.Equal("https://example.com/project", success.ProjectUrl);
        Assert.Equal("https://github.com/example/package", success.RepositoryUrl);

        Assert.Collection(
            success.OtherUrls,
            url =>
            {
                Assert.Equal("iconUrl", url.Source);
                Assert.Equal("https://example.com/icon.png", url.Url);
            },
            url =>
            {
                Assert.Equal("licenseUrl", url.Source);
                Assert.Equal("https://example.com/license", url.Url);
            },
            url =>
            {
                Assert.Equal("description", url.Source);
                Assert.Equal("https://example.com/docs", url.Url);
            },
            url =>
            {
                Assert.Equal("releaseNotes", url.Source);
                Assert.Equal("https://example.com/releases/1.0.0", url.Url);
            }
        );
    }

    [Fact]
    public void GetNuGetUrlsReturnsNotFoundWhenPackageIsMissing()
    {
        var fileSystem = new MockFileSystem();

        var result = new GetNuGetUrlsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetUrls("Missing.Package", "1.0.0");

        AssertNotFoundProblem(result, "was not found");
    }

    private static void AssertNotFoundProblem(NuGetToolResult result, string expectedDetail)
    {
        var problem = Assert.IsType<NuGetProblemDetailsResult>(result);
        Assert.Equal(ProblemTypes.NotFound, problem.Type);
        Assert.Equal("Not Found", problem.Title);
        Assert.Equal(404, problem.Status);
        Assert.Contains(expectedDetail, problem.Detail);
    }

    private static string GetPackageDirectory(string packageName, string version)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(
            userProfile,
            ".nuget",
            "packages",
            packageName.ToLowerInvariant(),
            version.ToLowerInvariant()
        );
    }
}
