using System.IO.Abstractions.TestingHelpers;
using Raiqub.NuSpec.Features.Common.Models;
using Raiqub.NuSpec.Features.Common.Services;
using Xunit;

namespace Raiqub.NuSpec.Tests.Features.Common.Services;

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

        AssertNotFoundProblem(result, "was not found");
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

        AssertNotFoundProblem(result, "no .nuspec file was found");
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

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal(expectedMetadata.Id, success.Id);
        Assert.Equal(expectedMetadata.Version, success.Version);
        Assert.True(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataUsesLatestFoundVersionWhenVersionIsNotProvided()
    {
        var oldPackageDirectory = GetPackageDirectory("Package.With.Metadata", "1.0.0");
        var prereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-beta");
        var latestPackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0");
        var nuspecPath = Path.Combine(latestPackageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(oldPackageDirectory);
        fileSystem.AddDirectory(prereleasePackageDirectory);
        fileSystem.AddDirectory(latestPackageDirectory);
        var parser = new RecordingNuGetPackageMetadataParser(expectedMetadata);

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0", success.Version);
        Assert.Equal(expectedMetadata.Id, success.Id);
        Assert.True(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataUsesLatestNumericPrereleaseIdentifierWhenVersionIsNotProvided()
    {
        var lowPrereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-beta.2");
        var highPrereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-beta.10");
        var nuspecPath = Path.Combine(highPrereleasePackageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0-beta.10" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(lowPrereleasePackageDirectory);
        fileSystem.AddDirectory(highPrereleasePackageDirectory);
        var parser = new RecordingNuGetPackageMetadataParser(expectedMetadata);

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0-beta.10", success.Version);
        Assert.Equal(expectedMetadata.Id, success.Id);
        Assert.True(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataIgnoresInvalidVersionDirectoriesWhenSelectingLatest()
    {
        var invalidVersionDirectory = GetPackageDirectory("Package.With.Metadata", "not-a-version");
        var prereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-beta");
        var latestPackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0");
        var nuspecPath = Path.Combine(latestPackageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(invalidVersionDirectory);
        fileSystem.AddDirectory(prereleasePackageDirectory);
        fileSystem.AddDirectory(latestPackageDirectory);
        var parser = new RecordingNuGetPackageMetadataParser(expectedMetadata);

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0", success.Version);
        Assert.Equal(expectedMetadata.Id, success.Id);
    }

    [Fact]
    public void GetNuGetPackageMetadataUsesStableVersionOverSamePrereleaseWhenSelectingLatest()
    {
        var prereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-beta.1");
        var stablePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0");
        var nuspecPath = Path.Combine(stablePackageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(prereleasePackageDirectory);
        fileSystem.AddDirectory(stablePackageDirectory);
        var parser = new RecordingNuGetPackageMetadataParser(expectedMetadata);

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0", success.Version);
        Assert.Equal(expectedMetadata.Id, success.Id);
    }

    [Fact]
    public void GetNuGetPackageMetadataUsesLatestPrereleaseIdentifierWithMoreSegmentsWhenSelectingLatest()
    {
        var longerPrereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-beta.1");
        var shorterPrereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-beta");
        var nuspecPath = Path.Combine(longerPrereleasePackageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0-beta.1" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(longerPrereleasePackageDirectory);
        fileSystem.AddDirectory(shorterPrereleasePackageDirectory);
        var parser = new RecordingNuGetPackageMetadataParser(expectedMetadata);

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0-beta.1", success.Version);
        Assert.Equal(expectedMetadata.Id, success.Id);
    }

    [Fact]
    public void GetNuGetPackageMetadataUsesNumericPrereleaseOrderingWithMixedIdentifiersWhenSelectingLatest()
    {
        var numericPrereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-alpha.10");
        var alphabeticPrereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-alpha.beta");
        var nuspecPath = Path.Combine(alphabeticPrereleasePackageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0-alpha.beta" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(numericPrereleasePackageDirectory);
        fileSystem.AddDirectory(alphabeticPrereleasePackageDirectory);
        var parser = new RecordingNuGetPackageMetadataParser(expectedMetadata);

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0-alpha.beta", success.Version);
        Assert.Equal(expectedMetadata.Id, success.Id);
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenRequestedVersionDirectoryIsMissing()
    {
        var fileSystem = new MockFileSystem();
        var parser = new RecordingNuGetPackageMetadataParser();

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Metadata",
            "1.0.0"
        );

        AssertNotFoundProblem(result, "version '1.0.0' was not found");
        Assert.False(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenVersionIsNotProvidedAndPackageIsMissing()
    {
        var fileSystem = new MockFileSystem();
        var parser = new RecordingNuGetPackageMetadataParser();

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata("Missing.Package");

        AssertNotFoundProblem(result, "was not found");
        Assert.False(parser.WasCalled);
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

        AssertNotFoundProblem(result, "multiple .nuspec files were found");
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

        AssertNotFoundProblem(result, "invalid or malformed .nuspec file");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenNuspecHasNoMetadataElement()
    {
        var packageDirectory = GetPackageDirectory("Package.Without.Metadata.Element", "1.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.without.metadata.element.nuspec");
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><content /></package>") }
        );
        fileSystem.AddDirectory(packageDirectory);

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new NuGetPackageMetadataParser()
        ).GetNuGetPackageMetadata("Package.Without.Metadata.Element", "1.0.0");

        AssertNotFoundProblem(result, "invalid or malformed .nuspec file");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsForbiddenWhenNuspecCannotBeOpened()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Protected.Nuspec", "1.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.with.protected.nuspec");
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(packageDirectory);
        var parser = new ThrowingNuGetPackageMetadataParser(new UnauthorizedAccessException("Denied."));

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Protected.Nuspec",
            "1.0.0"
        );

        AssertProblem(result, ProblemTypes.Forbidden, "Forbidden", 403, "was denied");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsInternalServerErrorWhenReadingNuspecFails()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Failing.Nuspec", "1.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.with.failing.nuspec");
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(packageDirectory);
        var parser = new ThrowingNuGetPackageMetadataParser(new IOException("Read failed."));

        var result = new NuGetPackageMetadataService(fileSystem, parser).GetNuGetPackageMetadata(
            "Package.With.Failing.Nuspec",
            "1.0.0"
        );

        AssertProblem(result, ProblemTypes.InternalServerError, "Internal Server Error", 500, "I/O error occurred");
    }

    private static void AssertNotFoundProblem(NuGetPackageMetadataLookup result, string expectedDetail)
    {
        AssertProblem(result, ProblemTypes.NotFound, "Not Found", 404, expectedDetail);
    }

    private static void AssertProblem(
        NuGetPackageMetadataLookup result,
        string expectedType,
        string expectedTitle,
        int expectedStatus,
        string expectedDetail
    )
    {
        Assert.Null(result.Metadata);
        Assert.NotNull(result.Problem);
        Assert.Equal(expectedType, result.Problem.Type);
        Assert.Equal(expectedTitle, result.Problem.Title);
        Assert.Equal(expectedStatus, result.Problem.Status);
        Assert.Contains(expectedDetail, result.Problem.Detail);
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

    private sealed class ThrowingNuGetPackageMetadataParser(Exception exception) : INuGetPackageMetadataParser
    {
        public NuGetPackageMetadata? Parse(Stream stream) => throw exception;
    }
}
