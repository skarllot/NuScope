using System.IO.Abstractions;
using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.Common.Services;

public sealed class NuGetPackageMetadataService : INuGetPackageMetadataService
{
    private readonly IFileSystem fileSystem;
    private readonly INuGetPackageMetadataParser parser;
    private readonly INuGetRemotePackageMetadataClient remoteMetadataClient;

    public NuGetPackageMetadataService(
        IFileSystem fileSystem,
        INuGetPackageMetadataParser parser,
        INuGetRemotePackageMetadataClient remoteMetadataClient
    )
    {
        this.fileSystem = fileSystem;
        this.parser = parser;
        this.remoteMetadataClient = remoteMetadataClient;
    }

    public NuGetPackageMetadataService(IFileSystem fileSystem, INuGetPackageMetadataParser parser)
        : this(fileSystem, parser, NoOpRemotePackageMetadataClient.Instance) { }

    public NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var resolvedVersion = version;
        if (string.IsNullOrWhiteSpace(resolvedVersion))
        {
            var latestVersionLookup = GetLatestPackageVersion(packageName);
            if (latestVersionLookup.Problem is not null)
            {
                return NuGetPackageMetadataLookup.FromProblem(latestVersionLookup.Problem);
            }

            resolvedVersion = latestVersionLookup.Versions![0];
        }

        var localLookup = GetLocalNuGetPackageMetadata(packageName, resolvedVersion);
        if (!localLookup.ShouldFallback)
        {
            return localLookup.Lookup;
        }

        var remoteLookup = remoteMetadataClient.GetNuGetPackageMetadata(packageName, resolvedVersion);
        return remoteLookup.Metadata is not null || remoteLookup.Problem is not null
            ? remoteLookup
            : localLookup.Lookup;
    }

    public NuGetPackageVersionsLookup GetNuGetPackageVersions(
        string packageName,
        int? minimumMajor = null,
        bool includePreRelease = false,
        int? maxItems = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        ThrowIfNegative(minimumMajor);
        ThrowIfNotPositive(maxItems);

        var localLookup = GetLocalNuGetPackageVersions(packageName, minimumMajor, includePreRelease);
        var remoteLookup = remoteMetadataClient.GetNuGetPackageVersions(
            packageName,
            minimumMajor,
            includePreRelease,
            maxItems
        );

        if (localLookup.Versions is not null || remoteLookup.Versions is not null)
        {
            var versions = MergeVersions(
                localLookup.Versions ?? [],
                remoteLookup.Versions ?? [],
                minimumMajor,
                includePreRelease,
                maxItems
            );
            return versions.Length == 0
                ? NuGetPackageVersionsLookup.NotFound(GetVersionsNotFoundDetail(packageName, minimumMajor))
                : NuGetPackageVersionsLookup.Found(versions);
        }

        if (remoteLookup.Problem is { Status: not 404 })
        {
            return remoteLookup;
        }

        if (localLookup.Problem is { Status: not 404 })
        {
            return localLookup;
        }

        return NuGetPackageVersionsLookup.NotFound(GetVersionsNotFoundDetail(packageName, minimumMajor));
    }

    private LocalNuGetPackageMetadataLookup GetLocalNuGetPackageMetadata(string packageName, string? version)
    {
        var resolvedVersion = version;

        try
        {
            if (string.IsNullOrWhiteSpace(resolvedVersion))
            {
                resolvedVersion = GetLatestVersion(packageName);
            }

            if (resolvedVersion is null)
            {
                return new LocalNuGetPackageMetadataLookup(
                    NuGetPackageMetadataLookup.NotFound(
                        $"Package '{packageName}' was not found in the local NuGet cache."
                    ),
                    ShouldFallback: true
                );
            }

            var packageDirectory = GetPackageDirectory(packageName, resolvedVersion);
            if (!fileSystem.Directory.Exists(packageDirectory))
            {
                return new LocalNuGetPackageMetadataLookup(
                    NuGetPackageMetadataLookup.NotFound(
                        $"Package '{packageName}' version '{resolvedVersion}' was not found in the local NuGet cache."
                    ),
                    ShouldFallback: true
                );
            }

            var nuspecPaths = fileSystem.Directory.EnumerateFiles(packageDirectory, "*.nuspec").ToArray();
            if (nuspecPaths.Length == 0)
            {
                return new LocalNuGetPackageMetadataLookup(
                    NuGetPackageMetadataLookup.NotFound(
                        $"Package '{packageName}' version '{resolvedVersion}' exists in the local NuGet cache, but no .nuspec file was found."
                    ),
                    ShouldFallback: false
                );
            }

            if (nuspecPaths.Length > 1)
            {
                return new LocalNuGetPackageMetadataLookup(
                    NuGetPackageMetadataLookup.NotFound(
                        $"Package '{packageName}' version '{resolvedVersion}' exists in the local NuGet cache, but multiple .nuspec files were found."
                    ),
                    ShouldFallback: false
                );
            }

            var nuspecPath = nuspecPaths[0];

            using var stream = fileSystem.File.OpenRead(nuspecPath);
            var metadata = parser.Parse(stream);
            if (metadata is null)
            {
                return new LocalNuGetPackageMetadataLookup(
                    NuGetPackageMetadataLookup.NotFound(
                        $"Package '{packageName}' version '{resolvedVersion}' has an invalid or malformed .nuspec file."
                    ),
                    ShouldFallback: false
                );
            }

            return new LocalNuGetPackageMetadataLookup(
                NuGetPackageMetadataLookup.Found(metadata),
                ShouldFallback: false
            );
        }
        catch (UnauthorizedAccessException)
        {
            return new LocalNuGetPackageMetadataLookup(
                NuGetPackageMetadataLookup.FromProblem(
                    NuGetProblemDetailsResult.Forbidden(
                        $"Access to {DescribeLocalPackageLookup(packageName, resolvedVersion)} in the local NuGet cache was denied."
                    )
                ),
                ShouldFallback: false
            );
        }
        catch (IOException)
        {
            return new LocalNuGetPackageMetadataLookup(
                NuGetPackageMetadataLookup.FromProblem(
                    NuGetProblemDetailsResult.InternalServerError(
                        $"An I/O error occurred while reading {DescribeLocalPackageLookup(packageName, resolvedVersion)} from the local NuGet cache."
                    )
                ),
                ShouldFallback: false
            );
        }
    }

    private string? GetLatestVersion(string packageName)
    {
        var packageRootDirectory = GetPackageRootDirectory(packageName);
        if (!fileSystem.Directory.Exists(packageRootDirectory))
        {
            return null;
        }

        return fileSystem
            .Directory.EnumerateDirectories(packageRootDirectory)
            .Select(fileSystem.Path.GetFileName)
            .OfType<string>()
            .Select(version => new { Version = version, Parsed = NuGetPackageVersion.TryParse(version) })
            .Where(version => version.Parsed is not null)
            .OrderByDescending(version => version.Parsed)
            .FirstOrDefault()
            ?.Version;
    }

    private NuGetPackageVersionsLookup GetLatestPackageVersion(string packageName)
    {
        var localVersionsLookup = GetLocalNuGetPackageVersions(
            packageName,
            minimumMajor: null,
            includePreRelease: true
        );
        if (localVersionsLookup.Problem is { Status: not 404 })
        {
            return localVersionsLookup;
        }

        var remoteVersionsLookup = remoteMetadataClient.GetNuGetPackageVersions(
            packageName,
            minimumMajor: null,
            includePreRelease: true
        );

        if (localVersionsLookup.Versions is not null || remoteVersionsLookup.Versions is not null)
        {
            var versions = MergeVersions(
                localVersionsLookup.Versions ?? [],
                remoteVersionsLookup.Versions ?? [],
                minimumMajor: null,
                includePreRelease: true,
                maxItems: 1
            );
            return versions.Length == 0
                ? NuGetPackageVersionsLookup.NotFound(GetVersionsNotFoundDetail(packageName, minimumMajor: null))
                : NuGetPackageVersionsLookup.Found(versions);
        }

        if (remoteVersionsLookup.Problem is { Status: not 404 })
        {
            return remoteVersionsLookup;
        }

        return NuGetPackageVersionsLookup.NotFound(GetVersionsNotFoundDetail(packageName, minimumMajor: null));
    }

    private NuGetPackageVersionsLookup GetLocalNuGetPackageVersions(
        string packageName,
        int? minimumMajor,
        bool includePreRelease
    )
    {
        try
        {
            var packageRootDirectory = GetPackageRootDirectory(packageName);
            if (!fileSystem.Directory.Exists(packageRootDirectory))
            {
                return NuGetPackageVersionsLookup.NotFound(
                    $"Package '{packageName}' was not found in the local NuGet cache."
                );
            }

            var versions = fileSystem
                .Directory.EnumerateDirectories(packageRootDirectory)
                .Select(fileSystem.Path.GetFileName)
                .OfType<string>()
                .Select(version => new { Version = version, Parsed = NuGetPackageVersion.TryParse(version) })
                .Where(version => VersionMatchesFilter(version.Parsed, minimumMajor, includePreRelease))
                .OrderByDescending(version => version.Parsed)
                .Select(version => version.Version)
                .ToArray();

            return versions.Length == 0
                ? NuGetPackageVersionsLookup.NotFound(GetVersionsNotFoundDetail(packageName, minimumMajor))
                : NuGetPackageVersionsLookup.Found(versions);
        }
        catch (UnauthorizedAccessException)
        {
            return NuGetPackageVersionsLookup.FromProblem(
                NuGetProblemDetailsResult.Forbidden(
                    $"Access to package '{packageName}' versions in the local NuGet cache was denied."
                )
            );
        }
        catch (IOException)
        {
            return NuGetPackageVersionsLookup.FromProblem(
                NuGetProblemDetailsResult.InternalServerError(
                    $"An I/O error occurred while reading package '{packageName}' versions from the local NuGet cache."
                )
            );
        }
    }

    private static string[] MergeVersions(
        IReadOnlyList<string> localVersions,
        IReadOnlyList<string> remoteVersions,
        int? minimumMajor,
        bool includePreRelease,
        int? maxItems
    )
    {
        var versions = localVersions
            .Concat(remoteVersions)
            .Select(version => new { Version = version, Parsed = NuGetPackageVersion.TryParse(version) })
            .Where(version => VersionMatchesFilter(version.Parsed, minimumMajor, includePreRelease))
            .GroupBy(version => version.Version, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(version => version.Parsed)
            .Select(version => version.Version);

        if (maxItems is not null)
        {
            versions = versions.Take(maxItems.Value);
        }

        return versions.ToArray();
    }

    private static bool VersionMatchesFilter(NuGetPackageVersion? version, int? minimumMajor, bool includePreRelease) =>
        version is not null
        && (includePreRelease || version.Prerelease is null)
        && (minimumMajor is null || version.Release[0] >= minimumMajor.Value);

    private static void ThrowIfNegative(int? minimumMajor)
    {
        if (minimumMajor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumMajor),
                minimumMajor,
                "Minimum major must be non-negative."
            );
        }
    }

    private static void ThrowIfNotPositive(int? maxItems)
    {
        if (maxItems < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItems), maxItems, "Maximum items must be positive.");
        }
    }

    private string GetPackageDirectory(string packageName, string version)
    {
        return fileSystem.Path.Combine(GetPackageRootDirectory(packageName), version.ToLowerInvariant());
    }

    private string GetPackageRootDirectory(string packageName)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return fileSystem.Path.Combine(userProfile, ".nuget", "packages", packageName.ToLowerInvariant());
    }

    private static string DescribeLocalPackageLookup(string packageName, string? resolvedVersion)
    {
        return resolvedVersion is null
            ? $"package '{packageName}' while resolving the latest available version"
            : $"package '{packageName}' version '{resolvedVersion}'";
    }

    private sealed record LocalNuGetPackageMetadataLookup(NuGetPackageMetadataLookup Lookup, bool ShouldFallback);

    private sealed class NoOpRemotePackageMetadataClient : INuGetRemotePackageMetadataClient
    {
        public static readonly NoOpRemotePackageMetadataClient Instance = new();

        public NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null)
        {
            return string.IsNullOrWhiteSpace(version)
                ? NuGetPackageMetadataLookup.NotFound(
                    $"Package '{packageName}' was not found in the local NuGet cache."
                )
                : NuGetPackageMetadataLookup.NotFound(
                    $"Package '{packageName}' version '{version}' was not found in the local NuGet cache."
                );
        }

        public NuGetPackageVersionsLookup GetNuGetPackageVersions(
            string packageName,
            int? minimumMajor = null,
            bool includePreRelease = false,
            int? maxItems = null
        )
        {
            return NuGetPackageVersionsLookup.NotFound(GetVersionsNotFoundDetail(packageName, minimumMajor));
        }
    }

    private static string GetVersionsNotFoundDetail(string packageName, int? minimumMajor)
    {
        return minimumMajor is null
            ? $"Package '{packageName}' versions were not found."
            : $"Package '{packageName}' versions with major version >= {minimumMajor.Value} were not found.";
    }
}
