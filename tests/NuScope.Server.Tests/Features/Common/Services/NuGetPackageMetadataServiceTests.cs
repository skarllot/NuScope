using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.Common.Services;

public sealed class NuGetPackageMetadataServiceTests
{
    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenPackageIsMissing()
    {
        var fileSystem = new MockFileSystem();
        var parser = new RecordingNuGetPackageMetadataParser();
        var remoteClient = new RecordingRemotePackageMetadataClient();

        var result = new NuGetPackageMetadataService(fileSystem, parser, remoteClient).GetNuGetPackageMetadata(
            "Missing.Package",
            "1.0.0"
        );

        Assert.Equal(remoteClient.Result, result);
        Assert.Equal("Missing.Package", remoteClient.PackageName);
        Assert.Equal("1.0.0", remoteClient.Version);
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
    public void GetNuGetPackageMetadataUsesLatestRemoteVersionWhenRemoteIsNewer()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Metadata", "1.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.with.metadata.nuspec");
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(packageDirectory);
        var remoteMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0" };
        var remoteClient = new RecordingRemotePackageMetadataClient(
            NuGetPackageMetadataLookup.Found(remoteMetadata),
            NuGetPackageVersionsLookup.Found(["2.0.0"])
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(new NuGetPackageMetadata { Id = "Ignored", Version = "1.0.0" }),
            remoteClient
        ).GetNuGetPackageMetadata("Package.With.Metadata");

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0", success.Version);
        Assert.Equal("Package.With.Metadata", remoteClient.PackageName);
        Assert.Equal("2.0.0", remoteClient.Version);
        Assert.Equal("Package.With.Metadata", remoteClient.VersionsPackageName);
        Assert.True(remoteClient.IncludePreRelease);
    }

    [Fact]
    public void GetNuGetPackageMetadataUsesLatestLocalVersionWhenRemoteIsOlder()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(packageDirectory);
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.Found(["1.0.0"])
        );

        var parser = new RecordingNuGetPackageMetadataParser(expectedMetadata);
        var result = new NuGetPackageMetadataService(fileSystem, parser, remoteClient).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0", success.Version);
        Assert.Null(remoteClient.PackageName);
        Assert.True(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataUsesLocalLatestWhenRemoteVersionLookupFails()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(packageDirectory);
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable("nuget.org failed.")
            )
        );

        var parser = new RecordingNuGetPackageMetadataParser(expectedMetadata);
        var result = new NuGetPackageMetadataService(fileSystem, parser, remoteClient).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        var success = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0", success.Version);
        Assert.Null(remoteClient.PackageName);
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsRemoteVersionLookupProblemWhenNoLocalVersionsExist()
    {
        var fileSystem = new MockFileSystem();
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable("nuget.org failed.")
            )
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageMetadata("Package.With.Metadata");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, "Service Unavailable", 503, "nuget.org failed");
        Assert.Null(remoteClient.PackageName);
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenMergedLatestVersionsAreInvalid()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Metadata");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "not-a-version"));
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.Found(["also-invalid"])
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageMetadata("Package.With.Metadata");

        AssertNotFoundProblem(result, "versions were not found");
        Assert.Null(remoteClient.PackageName);
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
        var emptyReleaseVersionDirectory = GetPackageDirectory("Package.With.Metadata", "1..0");
        var negativeReleaseVersionDirectory = GetPackageDirectory("Package.With.Metadata", "1.-1.0");
        var prereleasePackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0-beta");
        var latestPackageDirectory = GetPackageDirectory("Package.With.Metadata", "2.0.0");
        var nuspecPath = Path.Combine(latestPackageDirectory, "package.with.metadata.nuspec");
        var expectedMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "2.0.0" };
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata /></package>") }
        );
        fileSystem.AddDirectory(invalidVersionDirectory);
        fileSystem.AddDirectory(emptyReleaseVersionDirectory);
        fileSystem.AddDirectory(negativeReleaseVersionDirectory);
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
    public void GetNuGetPackageMetadataReturnsInternalServerErrorWhenResolvingLatestVersionFails()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Metadata");
        var innerFileSystem = new MockFileSystem();
        innerFileSystem.AddDirectory(packageRootDirectory);
        var fileSystem = new ThrowingOnEnumerateFileSystem(
            innerFileSystem,
            packageRootDirectory,
            new IOException("Read failed.")
        );
        var parser = new RecordingNuGetPackageMetadataParser();
        var remoteClient = new RecordingRemotePackageMetadataClient(
            NuGetPackageMetadataLookup.Found(new NuGetPackageMetadata { Id = "Ignored", Version = "9.9.9" })
        );

        var result = new NuGetPackageMetadataService(fileSystem, parser, remoteClient).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        AssertProblem(
            result,
            ProblemTypes.InternalServerError,
            "Internal Server Error",
            500,
            "reading package 'Package.With.Metadata' versions"
        );
        Assert.Null(remoteClient.PackageName);
        Assert.False(parser.WasCalled);
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
        var remoteClient = new RecordingRemotePackageMetadataClient();

        var result = new NuGetPackageMetadataService(fileSystem, parser, remoteClient).GetNuGetPackageMetadata(
            "Missing.Package"
        );

        AssertNotFoundProblem(result, "versions were not found");
        Assert.Null(remoteClient.PackageName);
        Assert.Equal("Missing.Package", remoteClient.VersionsPackageName);
        Assert.False(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataFallsBackToRemoteWhenRequestedVersionDirectoryIsMissing()
    {
        var fileSystem = new MockFileSystem();
        var parser = new RecordingNuGetPackageMetadataParser();
        var remoteMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "1.0.0" };
        var remoteClient = new RecordingRemotePackageMetadataClient(NuGetPackageMetadataLookup.Found(remoteMetadata));

        var result = new NuGetPackageMetadataService(fileSystem, parser, remoteClient).GetNuGetPackageMetadata(
            "Package.With.Metadata",
            "1.0.0"
        );

        var metadata = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal(remoteMetadata.Id, metadata.Id);
        Assert.Equal(remoteMetadata.Version, metadata.Version);
        Assert.Equal("Package.With.Metadata", remoteClient.PackageName);
        Assert.Equal("1.0.0", remoteClient.Version);
        Assert.False(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataFallsBackToRemoteWhenNoLocalVersionCanBeResolved()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Metadata");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(packageRootDirectory);
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "not-a-version"));
        var parser = new RecordingNuGetPackageMetadataParser();
        var remoteMetadata = new NuGetPackageMetadata { Id = "Package.With.Metadata", Version = "3.0.0" };
        var remoteClient = new RecordingRemotePackageMetadataClient(
            NuGetPackageMetadataLookup.Found(remoteMetadata),
            NuGetPackageVersionsLookup.Found(["3.0.0"])
        );

        var result = new NuGetPackageMetadataService(fileSystem, parser, remoteClient).GetNuGetPackageMetadata(
            "Package.With.Metadata"
        );

        var metadata = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("3.0.0", metadata.Version);
        Assert.Equal("Package.With.Metadata", remoteClient.PackageName);
        Assert.Equal("3.0.0", remoteClient.Version);
        Assert.False(parser.WasCalled);
    }

    [Fact]
    public void GetNuGetPackageMetadataDoesNotFallBackWhenLocalNuspecIsMalformed()
    {
        var packageDirectory = GetPackageDirectory("Package.With.Malformed.Nuspec", "1.0.0");
        var nuspecPath = Path.Combine(packageDirectory, "package.with.malformed.nuspec");
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [nuspecPath] = new MockFileData("<package><metadata></package>") }
        );
        fileSystem.AddDirectory(packageDirectory);
        var remoteClient = new RecordingRemotePackageMetadataClient(
            NuGetPackageMetadataLookup.Found(new NuGetPackageMetadata { Id = "Ignored", Version = "9.9.9" })
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new NuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageMetadata("Package.With.Malformed.Nuspec", "1.0.0");

        AssertNotFoundProblem(result, "invalid or malformed .nuspec file");
        Assert.Null(remoteClient.PackageName);
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

    [Fact]
    public void GetNuGetPackageVersionsReturnsValidStableLocalVersionsSortedDescending()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "2.0.0-beta"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "2.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "not-a-version"));

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser()
        ).GetNuGetPackageVersions("Package.With.Versions");

        Assert.Null(result.Problem);
        Assert.Equal(["2.0.0", "1.0.0"], result.Versions);
    }

    [Fact]
    public void GetNuGetPackageVersionsIncludesPrereleaseLocalVersionsWhenRequested()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "2.0.0-beta"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "2.0.0"));

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser()
        ).GetNuGetPackageVersions("Package.With.Versions", includePreRelease: true);

        Assert.Null(result.Problem);
        Assert.Equal(["2.0.0", "2.0.0-beta", "1.0.0"], result.Versions);
    }

    [Fact]
    public void GetNuGetPackageVersionsFiltersLocalVersionsByMinimumMajor()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "2.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "3.0.0"));

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser()
        ).GetNuGetPackageVersions("Package.With.Versions", minimumMajor: 2);

        Assert.Null(result.Problem);
        Assert.Equal(["3.0.0", "2.0.0"], result.Versions);
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsNotFoundWhenPackageIsAbsentOrFilteredOut()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0"));

        var absentResult = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser()
        ).GetNuGetPackageVersions("Missing.Package");
        var filteredResult = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser()
        ).GetNuGetPackageVersions("Package.With.Versions", minimumMajor: 2);

        AssertVersionsProblem(absentResult, ProblemTypes.NotFound, 404, "versions were not found");
        AssertVersionsProblem(filteredResult, ProblemTypes.NotFound, 404, "major version >= 2");
    }

    [Fact]
    public void GetNuGetPackageVersionsMergesAndDeduplicatesLocalAndRemoteVersions()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "2.0.0"));
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.Found(["2.0.0", "3.0.0"])
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageVersions("Package.With.Versions");

        Assert.Null(result.Problem);
        Assert.Equal(["3.0.0", "2.0.0", "1.0.0"], result.Versions);
        Assert.Equal("Package.With.Versions", remoteClient.VersionsPackageName);
        Assert.Null(remoteClient.MinimumMajor);
        Assert.False(remoteClient.IncludePreRelease);
        Assert.Null(remoteClient.MaxItems);
    }

    [Fact]
    public void GetNuGetPackageVersionsLimitsMergedVersionsAfterSorting()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "2.0.0"));
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.Found(["3.0.0", "4.0.0"])
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageVersions("Package.With.Versions", maxItems: 2);

        Assert.Null(result.Problem);
        Assert.Equal(["4.0.0", "3.0.0"], result.Versions);
        Assert.Equal(2, remoteClient.MaxItems);
    }

    [Fact]
    public void GetNuGetPackageVersionsMergesPrereleaseVersionsWhenRequested()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0-beta"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0"));
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.Found(["2.0.0-beta", "2.0.0"])
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageVersions("Package.With.Versions", includePreRelease: true);

        Assert.Null(result.Problem);
        Assert.Equal(["2.0.0", "2.0.0-beta", "1.0.0", "1.0.0-beta"], result.Versions);
        Assert.True(remoteClient.IncludePreRelease);
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsLocalVersionsWhenRemoteFails()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0"));
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable("nuget.org failed.")
            )
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageVersions("Package.With.Versions");

        Assert.Null(result.Problem);
        Assert.Equal(["1.0.0"], result.Versions);
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsRemoteVersionsWhenLocalPackageIsAbsent()
    {
        var fileSystem = new MockFileSystem();
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.Found(["2.0.0", "1.0.0"])
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageVersions("Package.With.Versions");

        Assert.Null(result.Problem);
        Assert.Equal(["2.0.0", "1.0.0"], result.Versions);
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsRemoteProblemWhenLocalPackageIsAbsent()
    {
        var fileSystem = new MockFileSystem();
        var remoteClient = new RecordingRemotePackageMetadataClient(
            versionsResult: NuGetPackageVersionsLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable("nuget.org failed.")
            )
        );

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageVersions("Package.With.Versions");

        AssertVersionsProblem(result, ProblemTypes.ServiceUnavailable, 503, "nuget.org failed");
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsLocalProblemWhenLocalEnumerationFails()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var innerFileSystem = new MockFileSystem();
        innerFileSystem.AddDirectory(packageRootDirectory);
        var fileSystem = new ThrowingOnEnumerateFileSystem(
            innerFileSystem,
            packageRootDirectory,
            new IOException("Read failed.")
        );
        var remoteClient = new RecordingRemotePackageMetadataClient();

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageVersions("Package.With.Versions");

        AssertVersionsProblem(result, ProblemTypes.InternalServerError, 500, "I/O error occurred");
        Assert.Equal("Package.With.Versions", remoteClient.VersionsPackageName);
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsForbiddenWhenLocalEnumerationIsDenied()
    {
        var packageRootDirectory = GetPackageRootDirectory("Package.With.Versions");
        var innerFileSystem = new MockFileSystem();
        innerFileSystem.AddDirectory(packageRootDirectory);
        var fileSystem = new ThrowingOnEnumerateFileSystem(
            innerFileSystem,
            packageRootDirectory,
            new UnauthorizedAccessException("Denied.")
        );
        var remoteClient = new RecordingRemotePackageMetadataClient();

        var result = new NuGetPackageMetadataService(
            fileSystem,
            new RecordingNuGetPackageMetadataParser(),
            remoteClient
        ).GetNuGetPackageVersions("Package.With.Versions");

        AssertVersionsProblem(result, ProblemTypes.Forbidden, 403, "was denied");
        Assert.Equal("Package.With.Versions", remoteClient.VersionsPackageName);
    }

    [Fact]
    public void GetNuGetPackageVersionsRejectsNegativeMinimumMajor()
    {
        var service = new NuGetPackageMetadataService(new MockFileSystem(), new RecordingNuGetPackageMetadataParser());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.GetNuGetPackageVersions("Package.With.Versions", minimumMajor: -1)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetNuGetPackageVersionsRejectsNonPositiveMaxItems(int maxItems)
    {
        var service = new NuGetPackageMetadataService(new MockFileSystem(), new RecordingNuGetPackageMetadataParser());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.GetNuGetPackageVersions("Package.With.Versions", maxItems: maxItems)
        );
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

    private static void AssertVersionsProblem(
        NuGetPackageVersionsLookup result,
        string expectedType,
        int expectedStatus,
        string expectedDetail
    )
    {
        Assert.Null(result.Versions);
        var problem = Assert.IsType<NuGetProblemDetailsResult>(result.Problem);
        Assert.Equal(expectedType, problem.Type);
        Assert.Equal(expectedStatus, problem.Status);
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

    private static string GetPackageRootDirectory(string packageName)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages", packageName.ToLowerInvariant());
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

    private sealed class RecordingRemotePackageMetadataClient(
        NuGetPackageMetadataLookup? result = null,
        NuGetPackageVersionsLookup? versionsResult = null
    ) : INuGetRemotePackageMetadataClient
    {
        public string? PackageName { get; private set; }

        public string? Version { get; private set; }

        public string? VersionsPackageName { get; private set; }

        public int? MinimumMajor { get; private set; }

        public bool IncludePreRelease { get; private set; }

        public int? MaxItems { get; private set; }

        public NuGetPackageMetadataLookup Result { get; } =
            result ?? NuGetPackageMetadataLookup.NotFound("Package was not found on nuget.org.");

        public NuGetPackageVersionsLookup VersionsResult { get; } =
            versionsResult ?? NuGetPackageVersionsLookup.NotFound("Package versions were not found on nuget.org.");

        public NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null)
        {
            PackageName = packageName;
            Version = version;
            return Result;
        }

        public NuGetPackageVersionsLookup GetNuGetPackageVersions(
            string packageName,
            int? minimumMajor = null,
            bool includePreRelease = false,
            int? maxItems = null
        )
        {
            VersionsPackageName = packageName;
            MinimumMajor = minimumMajor;
            IncludePreRelease = includePreRelease;
            MaxItems = maxItems;
            return VersionsResult;
        }
    }

    private sealed class ThrowingOnEnumerateFileSystem : FileSystemBase
    {
        private readonly IFileSystem innerFileSystem;

        public ThrowingOnEnumerateFileSystem(IFileSystem innerFileSystem, string pathToThrow, Exception exception)
        {
            this.innerFileSystem = innerFileSystem;
            Directory = new ThrowingOnEnumerateMockDirectory(
                (IMockFileDataAccessor)innerFileSystem,
                pathToThrow,
                exception
            );
        }

        public override IDirectory Directory { get; }

        public override IFile File => innerFileSystem.File;

        public override IFileInfoFactory FileInfo => innerFileSystem.FileInfo;

        public override IFileVersionInfoFactory FileVersionInfo => innerFileSystem.FileVersionInfo;

        public override IFileStreamFactory FileStream => innerFileSystem.FileStream;

        public override IPath Path => innerFileSystem.Path;

        public override IDirectoryInfoFactory DirectoryInfo => innerFileSystem.DirectoryInfo;

        public override IDriveInfoFactory DriveInfo => innerFileSystem.DriveInfo;

        public override IFileSystemWatcherFactory FileSystemWatcher => innerFileSystem.FileSystemWatcher;
    }

    private sealed class ThrowingOnEnumerateMockDirectory(
        IMockFileDataAccessor fileDataAccessor,
        string pathToThrow,
        Exception exception
    ) : MockDirectory(fileDataAccessor, Directory.GetCurrentDirectory())
    {
        public override IEnumerable<string> EnumerateDirectories(string path)
        {
            if (string.Equals(path, pathToThrow, StringComparison.OrdinalIgnoreCase))
            {
                throw exception;
            }

            return base.EnumerateDirectories(path);
        }
    }
}
