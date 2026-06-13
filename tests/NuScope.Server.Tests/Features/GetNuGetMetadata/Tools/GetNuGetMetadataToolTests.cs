using System.IO.Abstractions.TestingHelpers;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.GetNuGetMetadata.Models;
using Raiqub.NuScope.Features.GetNuGetMetadata.Tools;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetNuGetMetadata.Tools;

public sealed class GetNuGetMetadataToolTests
{
    [Fact]
    public void GetNuGetMetadataReturnsParsedMetadataWhenPackageExists()
    {
        var packageDirectory = GetPackageDirectory("Newtonsoft.Json", "13.0.3");
        var nuspecPath = Path.Combine(packageDirectory, "newtonsoft.json.nuspec");
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                [nuspecPath] = new MockFileData(
                    """
                    <?xml version="1.0" encoding="utf-8"?>
                    <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                      <metadata>
                        <id>Newtonsoft.Json</id>
                        <version>13.0.3</version>
                        <title>Json.NET</title>
                        <authors>James Newton-King</authors>
                        <description>JSON framework for .NET</description>
                        <license type="expression">MIT</license>
                        <repository type="git" url="https://github.com/JamesNK/Newtonsoft.Json" branch="master" commit="0123456789abcdef" />
                        <tags>json serialization parsing</tags>
                        <dependencies>
                          <group targetFramework="net8.0">
                            <dependency id="System.Text.Json" version="[8.0.0, )" />
                            <dependency id="System.Runtime" version="[8.0.0, )" exclude="Compile" />
                          </group>
                        </dependencies>
                      </metadata>
                    </package>
                    """
                ),
            }
        );

        fileSystem.AddDirectory(packageDirectory);

        var result = new GetNuGetMetadataTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetMetadata("Newtonsoft.Json");

        var success = Assert.IsType<NuGetMetadataResult>(result);
        Assert.Equal("Newtonsoft.Json", success.Id);
        Assert.Equal("13.0.3", success.Version);
        Assert.Equal("Json.NET", success.Title);
        Assert.Equal("James Newton-King", success.Authors);
        Assert.Equal("JSON framework for .NET", success.Description);
        Assert.Equal("expression", success.LicenseType);
        Assert.Equal("MIT", success.License);
        Assert.NotNull(success.Repository);
        Assert.Equal("git", success.Repository!.Type);
        Assert.Equal("https://github.com/JamesNK/Newtonsoft.Json", success.Repository.Url);
        Assert.Equal("master", success.Repository.Branch);
        Assert.Equal("0123456789abcdef", success.Repository.Commit);
        Assert.Equal(["json", "serialization", "parsing"], success.Tags);

        var dependencyGroup = Assert.Single(success.DependencyGroups);
        Assert.Equal("net8.0", dependencyGroup.TargetFramework);

        Assert.Equal(2, dependencyGroup.Dependencies.Count);
        Assert.Equal("System.Text.Json", dependencyGroup.Dependencies[0].Id);
        Assert.Equal("[8.0.0, )", dependencyGroup.Dependencies[0].Version);
        Assert.Null(dependencyGroup.Dependencies[0].Exclude);
        Assert.Equal("System.Runtime", dependencyGroup.Dependencies[1].Id);
        Assert.Equal("[8.0.0, )", dependencyGroup.Dependencies[1].Version);
        Assert.Equal("Compile", dependencyGroup.Dependencies[1].Exclude);
    }

    [Fact]
    public void GetNuGetMetadataReturnsNotFoundWhenPackageIsMissing()
    {
        var fileSystem = new MockFileSystem();

        var result = new GetNuGetMetadataTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetMetadata("Missing.Package", "1.0.0");

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
