using System.Globalization;
using System.IO.Abstractions;
using Raiqub.NuSpec.Models;

namespace Raiqub.NuSpec.Services;

public sealed class NuGetPackageMetadataService(IFileSystem fileSystem, INuGetPackageMetadataParser parser)
    : INuGetPackageMetadataService
{
    public NuGetPackageMetadataResult GetNuGetPackageMetadata(string packageName, string? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var resolvedVersion = string.IsNullOrWhiteSpace(version) ? GetLatestVersion(packageName) : version;
        if (resolvedVersion is null)
        {
            return NuGetPackageMetadataResult.NotFound(
                packageName,
                string.Empty,
                $"Package '{packageName}' was not found in the local NuGet cache."
            );
        }

        var packageDirectory = GetPackageDirectory(packageName, resolvedVersion);
        if (!fileSystem.Directory.Exists(packageDirectory))
        {
            return NuGetPackageMetadataResult.NotFound(
                packageName,
                resolvedVersion,
                $"Package '{packageName}' version '{resolvedVersion}' was not found in the local NuGet cache."
            );
        }

        var nuspecPaths = fileSystem.Directory.EnumerateFiles(packageDirectory, "*.nuspec").ToArray();
        if (nuspecPaths.Length == 0)
        {
            return NuGetPackageMetadataResult.NotFound(
                packageName,
                resolvedVersion,
                $"Package '{packageName}' version '{resolvedVersion}' exists in the local NuGet cache, but no .nuspec file was found."
            );
        }

        if (nuspecPaths.Length > 1)
        {
            return NuGetPackageMetadataResult.NotFound(
                packageName,
                resolvedVersion,
                $"Package '{packageName}' version '{resolvedVersion}' exists in the local NuGet cache, but multiple .nuspec files were found."
            );
        }

        var nuspecPath = nuspecPaths[0];

        using var stream = fileSystem.File.OpenRead(nuspecPath);
        var metadata = parser.Parse(stream);
        if (metadata is null)
        {
            return NuGetPackageMetadataResult.NotFound(
                packageName,
                resolvedVersion,
                $"Package '{packageName}' version '{resolvedVersion}' has an invalid .nuspec file with no metadata element."
            );
        }

        return NuGetPackageMetadataResult.Found(packageName, resolvedVersion, packageDirectory, nuspecPath, metadata);
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
            .Select(version => new { Version = version, Parsed = PackageVersion.TryParse(version) })
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

    private sealed record PackageVersion(int[] Release, string? Prerelease) : IComparable<PackageVersion>
    {
        public static PackageVersion? TryParse(string version)
        {
            var versionWithoutMetadata = version.Split('+', 2)[0];
            var parts = versionWithoutMetadata.Split('-', 2);
            var release = parts[0]
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => int.TryParse(part, out var parsed) ? parsed : (int?)null)
                .ToArray();

            return release.Length == 0 || release.Any(part => part is null)
                ? null
                : new PackageVersion(release.Select(part => part!.Value).ToArray(), parts.Length > 1 ? parts[1] : null);
        }

        public int CompareTo(PackageVersion? other)
        {
            if (other is null)
            {
                return 1;
            }

            var releaseLength = Math.Max(Release.Length, other.Release.Length);
            for (var index = 0; index < releaseLength; index++)
            {
                var comparison = GetReleasePart(index).CompareTo(other.GetReleasePart(index));
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return ComparePrerelease(other);
        }

        private int ComparePrerelease(PackageVersion other)
        {
            return (Prerelease, other.Prerelease) switch
            {
                (null, null) => 0,
                (null, _) => 1,
                (_, null) => -1,
                _ => ComparePrereleaseIdentifiers(Prerelease, other.Prerelease),
            };
        }

        private int GetReleasePart(int index) => index < Release.Length ? Release[index] : 0;

        private static int ComparePrereleaseIdentifiers(string prerelease, string otherPrerelease)
        {
            var identifiers = prerelease.Split('.');
            var otherIdentifiers = otherPrerelease.Split('.');
            var identifierLength = Math.Max(identifiers.Length, otherIdentifiers.Length);

            for (var index = 0; index < identifierLength; index++)
            {
                if (index >= identifiers.Length)
                {
                    return -1;
                }

                if (index >= otherIdentifiers.Length)
                {
                    return 1;
                }

                var identifier = identifiers[index];
                var otherIdentifier = otherIdentifiers[index];
                var identifierIsNumeric = IsNumericIdentifier(identifier);
                var otherIdentifierIsNumeric = IsNumericIdentifier(otherIdentifier);

                var comparison = (identifierIsNumeric, otherIdentifierIsNumeric) switch
                {
                    (true, false) => -1,
                    (false, true) => 1,
                    _ => CultureInfo.InvariantCulture.CompareInfo.Compare(
                        identifier,
                        otherIdentifier,
                        CompareOptions.IgnoreCase | CompareOptions.NumericOrdering
                    ),
                };

                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        private static bool IsNumericIdentifier(string identifier) =>
            identifier.Length > 0 && identifier.All(char.IsAsciiDigit);
    }
}
