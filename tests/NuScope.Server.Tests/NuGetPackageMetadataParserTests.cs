using Raiqub.NuSpec.Services;
using Xunit;

namespace Raiqub.NuSpec.Tests;

public sealed class NuGetPackageMetadataParserTests
{
    [Fact]
    public void ParseReturnsDependencyGroupForDirectDependencies()
    {
        using var stream = CreateStream(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <package>
              <metadata>
                <id>Package</id>
                <dependencies>
                  <dependency id="First.Dependency" version="1.0.0" />
                  <dependency id="Second.Dependency" version="[2.0.0, )" exclude="Compile" />
                </dependencies>
              </metadata>
            </package>
            """
        );

        var metadata = new NuGetPackageMetadataParser().Parse(stream);

        Assert.NotNull(metadata);
        var dependencyGroup = Assert.Single(metadata!.DependencyGroups);
        Assert.Null(dependencyGroup.TargetFramework);
        Assert.Equal(2, dependencyGroup.Dependencies.Count);
        Assert.Equal("First.Dependency", dependencyGroup.Dependencies[0].Id);
        Assert.Equal("1.0.0", dependencyGroup.Dependencies[0].Version);
        Assert.Null(dependencyGroup.Dependencies[0].Exclude);
        Assert.Equal("Second.Dependency", dependencyGroup.Dependencies[1].Id);
        Assert.Equal("[2.0.0, )", dependencyGroup.Dependencies[1].Version);
        Assert.Equal("Compile", dependencyGroup.Dependencies[1].Exclude);
    }

    [Fact]
    public void ParseReturnsNullWhenRepositoryElementIsMissing()
    {
        using var stream = CreateStream(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <package>
              <metadata>
                <id>Package</id>
              </metadata>
            </package>
            """
        );

        var metadata = new NuGetPackageMetadataParser().Parse(stream);

        Assert.NotNull(metadata);
        Assert.Null(metadata!.Repository);
    }

    [Fact]
    public void ParseReturnsPartialRepositoryMetadataWhenSomeAttributesAreMissing()
    {
        using var stream = CreateStream(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <package>
              <metadata>
                <id>Package</id>
                <repository url="https://github.com/example/repo" />
              </metadata>
            </package>
            """
        );

        var metadata = new NuGetPackageMetadataParser().Parse(stream);

        Assert.NotNull(metadata);
        Assert.NotNull(metadata!.Repository);
        Assert.Null(metadata.Repository!.Type);
        Assert.Equal("https://github.com/example/repo", metadata.Repository.Url);
        Assert.Null(metadata.Repository.Branch);
        Assert.Null(metadata.Repository.Commit);
    }

    [Fact]
    public void ParseReturnsNullWhenMetadataElementIsMissing()
    {
        using var stream = CreateStream(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <package>
              <content />
            </package>
            """
        );

        var metadata = new NuGetPackageMetadataParser().Parse(stream);

        Assert.Null(metadata);
    }

    [Fact]
    public void ParseReturnsTagsLicenseAndGroupedDependencies()
    {
        using var stream = CreateStream(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <package>
              <metadata>
                <id>Package</id>
                <license type="file">LICENSE.txt</license>
                <repository type="git" url="https://github.com/example/repo" branch="main" commit="abcdef" />
                <requireLicenseAcceptance>true</requireLicenseAcceptance>
                <tags>one two  three</tags>
                <dependencies>
                  <group targetFramework="net8.0">
                    <dependency id="A" version="1.0.0" />
                  </group>
                  <group targetFramework="netstandard2.0">
                    <dependency id="B" version="2.0.0" exclude="Build" />
                  </group>
                </dependencies>
              </metadata>
            </package>
            """
        );

        var metadata = new NuGetPackageMetadataParser().Parse(stream);

        Assert.NotNull(metadata);
        Assert.Equal("file", metadata!.LicenseType);
        Assert.Equal("LICENSE.txt", metadata.License);
        Assert.NotNull(metadata.Repository);
        Assert.Equal("git", metadata.Repository!.Type);
        Assert.Equal("https://github.com/example/repo", metadata.Repository.Url);
        Assert.Equal("main", metadata.Repository.Branch);
        Assert.Equal("abcdef", metadata.Repository.Commit);
        Assert.True(metadata.RequireLicenseAcceptance);
        Assert.Equal(["one", "two", "three"], metadata.Tags);
        Assert.Equal(2, metadata.DependencyGroups.Count);
        Assert.Equal("net8.0", metadata.DependencyGroups[0].TargetFramework);
        Assert.Equal("A", metadata.DependencyGroups[0].Dependencies[0].Id);
        Assert.Equal("netstandard2.0", metadata.DependencyGroups[1].TargetFramework);
        Assert.Equal("Build", metadata.DependencyGroups[1].Dependencies[0].Exclude);
    }

    [Fact]
    public void ParseReturnsNullForMalformedXml()
    {
        using var stream = CreateStream("<package><metadata></package>");

        var metadata = new NuGetPackageMetadataParser().Parse(stream);

        Assert.Null(metadata);
    }

    private static MemoryStream CreateStream(string xml) => new(System.Text.Encoding.UTF8.GetBytes(xml));
}
