using System.IO.Abstractions;
using Raiqub.NuSpec.Features.Common.Models;

namespace Raiqub.NuSpec.Features.Common.Services;

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

        var localLookup = GetLocalNuGetPackageMetadata(packageName, version);
        if (!localLookup.ShouldFallback)
        {
            return localLookup.Lookup;
        }

        var remoteLookup = remoteMetadataClient.GetNuGetPackageMetadata(packageName, version);
        return remoteLookup.Metadata is not null || remoteLookup.Problem is not null
            ? remoteLookup
            : localLookup.Lookup;
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
    }
}
