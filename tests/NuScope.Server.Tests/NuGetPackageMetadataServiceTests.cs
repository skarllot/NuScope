using System.IO.Abstractions.TestingHelpers;
using Raiqub.NuSpec.Models;
using Raiqub.NuSpec.Services;
using Xunit;

namespace Raiqub.NuSpec.Tests;

public sealed class NuGetPackageMetadataServiceTests
{
    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenPackageIsMissing()
    {
        var fileSystem = new MockFileSystem();
        var parser = new RecordingNuGetPackageMetadataParser();

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Missing.Package",
            "1.0.0"
        );

        Assert.False(result.IsFound);
        Assert.Null(result.PackageDirectory);
        Assert.Null(result.NuspecPath);
        Assert.Null(result.Metadata);
        Assert.Contains("was not found", result.Message);
        Assert.False(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenPackageHasNoNuspec()
    {
        var packageDirectory = GetPackageDirectory("Package.Without.Nuspec", "1.0.0");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(packageDirectory);
        var parser = new RecordingNuGetPackageMetadataParser();

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.Without.Nuspec",
            "1.0.0"
        );

        Assert.False(result.IsFound);
        Assert.Null(result.PackageDirectory);
        Assert.Null(result.NuspecPath);
        Assert.Null(result.Metadata);
        Assert.Contains("no .nuspec file was found", result.Message);
        Assert.False(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsFoundWhenPackageHasValidNuspec()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(packageDirectory);
        var parser = new RecordingNuGetPackageMetadataParser(expectedMetadata);

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Metadata",
            "2.0.0"
        );

        Assert.True(result.IsFound);
        Assert.Equal(packageDirectory, result.PackageDirectory);
        Assert.Equal(nuspecPath, result.NuspecPath);
        Assert.Same(expectedMetadata, result.Metadata);
        Assert.True(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenMultipleNuspecFilesExist()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Multiple.Nuspec", "1.0.0");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(packageDirectory);
        fileSystem.AddFile(Path.Combine(packageDirectory, "one.nuspec"), new MockFileData("<package />"));
        fileSystem.AddFile(Path.Combine(packageDirectory, "two.nuspec"), new MockFileData("<package />"));
        var parser = new RecordingNuGetPackageMetadataParser();

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Multiple.Nuspec",
            "1.0.0"
        );

        Assert.False(result.IsFound);
        Assert.Null(result.NuspecPath);
        Assert.Contains("multiple .nuspec files were found", result.Message);
        Assert.False(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenNuspecIsMalformed()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Malformed.Nuspec", "1.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.with.malformed.nuspec");
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata></package>") }
        );
        fileSystem.AddDirectory(packageDirectory);

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new NuGetPackageMetadataParser()
        ).GetNuGetPackageMetadata("Package.With.Malformed.Nuspec", "1.0.0");

        Assert.False(result.IsFound);
        Assert.Null(result.Metadata);
        Assert.Contains("invalid .nuspec file", result.Message);
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

    private sealed class RecordingNuGetPackageMetadataParser(NuGetPackageMetadata? result = null)
        : INuGetPackageMetadataParser
    {
        public bool WasCalled { get; private set; }

        public NuGetPackageMetadata? Parse(Stream stream)
        {
            WasCalled = true;
            return result;
        }
    }
}
