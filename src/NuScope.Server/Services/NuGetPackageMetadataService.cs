using System.IO.Abstractions;
using Raiqub.NuSpec.Models;

namespace Raiqub.NuSpec.Services;

public sealed class NuGetPackageMetadataService(IFileSystem fileSystem, INuGetPackageMetadataParser parser)
    : INuGetPackageMetadataService
{
    public NuGetPackageMetadataResult GetNuGetPackageMetadata(string packageName, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var packageDirectory = GetPackageDirectory(packageName, version);
        if (!fileSystem.Directory.Exists(packageDirectory))
        {
            return NuGetPackageMetadataResult.NotFound(
                packageName,
                version,
                $"Package '{packageName}' version '{version}' was not found in the local NuGet cache."
            );
        }

        var nuspecPaths = fileSystem.Directory.EnumerateFiles(packageDirectory, "*.nuspec").ToArray();
        if (nuspecPaths.Length == 0)
        {
            return NuGetPackageMetadataResult.NotFound(
                packageName,
                version,
                $"Package '{packageName}' version '{version}' exists in the local NuGet cache, but no .nuspec file was found."
            );
        }

        if (nuspecPaths.Length > 1)
        {
            return NuGetPackageMetadataResult.NotFound(
                packageName,
                version,
                $"Package '{packageName}' version '{version}' exists in the local NuGet cache, but multiple .nuspec files were found."
            );
        }

        var nuspecPath = nuspecPaths[0];

        using var stream = fileSystem.File.OpenRead(nuspecPath);
        var metadata = parser.Parse(stream);
        if (metadata is null)
        {
            return NuGetPackageMetadataResult.NotFound(
                packageName,
                version,
                $"Package '{packageName}' version '{version}' has an invalid .nuspec file with no metadata element."
            );
        }

        return NuGetPackageMetadataResult.Found(packageName, version, packageDirectory, nuspecPath, metadata);
    }

    private string GetPackageDirectory(string packageName, string version)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return fileSystem.Path.Combine(
            userProfile,
            ".nuget",
            "packages",
            packageName.ToLowerInvariant(),
            version.ToLowerInvariant()
        );
    }
}
