using System.IO.Abstractions.TestingHelpers;
using Raiqub.NuSpec.Models;
using Raiqub.NuSpec.Services;
using Raiqub.NuSpec.Tools;
using Xunit;

namespace Raiqub.NuSpec.Tests;

public sealed class NuGetPackageMetadataToolsTests
{
    [Fact]
    public void GetNuGetPackageMetadataReturnsParsedMetadataWhenPackageExists()
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

        var result = new NuGetPackageMetadataTools(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetPackageMetadata("Newtonsoft.Json");

        Assert.True(result.IsFound);
        Assert.Equal("13.0.3", result.Version);
        Assert.Equal(packageDirectory, result.PackageDirectory);
        Assert.Equal(nuspecPath, result.NuspecPath);

        var metadata = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("Newtonsoft.Json", metadata.Id);
        Assert.Equal("13.0.3", metadata.Version);
        Assert.Equal("Json.NET", metadata.Title);
        Assert.Equal("James Newton-King", metadata.Authors);
        Assert.Equal("JSON framework for .NET", metadata.Description);
        Assert.Equal("expression", metadata.LicenseType);
        Assert.Equal("MIT", metadata.License);
        Assert.NotNull(metadata.Repository);
        Assert.Equal("git", metadata.Repository!.Type);
        Assert.Equal("https://github.com/JamesNK/Newtonsoft.Json", metadata.Repository.Url);
        Assert.Equal("master", metadata.Repository.Branch);
        Assert.Equal("0123456789abcdef", metadata.Repository.Commit);
        Assert.Equal(["json", "serialization", "parsing"], metadata.Tags);

        var dependencyGroup = Assert.Single(metadata.DependencyGroups);
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
    public void GetNuGetPackageMetadataReturnsNotFoundWhenPackageIsMissing()
    {
        var fileSystem = new MockFileSystem();

        var result = new NuGetPackageMetadataTools(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetPackageMetadata("Missing.Package", "1.0.0");

        Assert.False(result.IsFound);
        Assert.Null(result.Metadata);
        Assert.Contains("was not found", result.Message);
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
