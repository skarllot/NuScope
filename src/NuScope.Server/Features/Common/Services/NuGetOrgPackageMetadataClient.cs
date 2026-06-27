using System.Net;
using System.Text.Json;
using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.Common.Services;

public sealed class NuGetOrgPackageMetadataClient(HttpClient httpClient, INuGetPackageMetadataParser parser)
    : INuGetRemotePackageMetadataClient
{
    private static readonly Uri ServiceIndexUri = new("https://api.nuget.org/v3/index.json");

    public NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null)
    {
        var packageId = NormalizePackageId(packageName);

        try
        {
            var packageBaseAddress = GetPackageBaseAddress();
            if (packageBaseAddress is null)
            {
                return NuGetPackageMetadataLookup.FromProblem(
                    NuGetProblemDetailsResult.ServiceUnavailable(
                        "nuget.org did not advertise a package content endpoint."
                    )
                );
            }

            using var versionsResponse = Send(packageBaseAddress, $"{packageId}/index.json");
            if (versionsResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return NuGetPackageMetadataLookup.NotFound($"Package '{packageName}' was not found on nuget.org.");
            }

            if (!versionsResponse.IsSuccessStatusCode)
            {
                return NuGetPackageMetadataLookup.FromProblem(
                    NuGetProblemDetailsResult.ServiceUnavailable(
                        $"nuget.org returned {(int)versionsResponse.StatusCode} while looking up package '{packageName}'."
                    )
                );
            }

            string[]? versions;
            using var versionsStream = versionsResponse.Content.ReadAsStream();
            using var versionsDocument = JsonDocument.Parse(versionsStream);
            versions = GetVersions(versionsDocument.RootElement);

            if (versions is null)
            {
                return NuGetPackageMetadataLookup.FromProblem(
                    NuGetProblemDetailsResult.ServiceUnavailable(
                        $"nuget.org returned an invalid versions response for package '{packageName}'."
                    )
                );
            }

            var resolvedVersion = ResolveVersion(versions, version);
            if (resolvedVersion is null)
            {
                return string.IsNullOrWhiteSpace(version)
                    ? NuGetPackageMetadataLookup.NotFound($"Package '{packageName}' was not found on nuget.org.")
                    : NuGetPackageMetadataLookup.NotFound(
                        $"Package '{packageName}' version '{version}' was not found on nuget.org."
                    );
            }

            using var nuspecResponse = Send(
                packageBaseAddress,
                $"{packageId}/{resolvedVersion.ToLowerInvariant()}/{packageId}.nuspec"
            );
            if (nuspecResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return NuGetPackageMetadataLookup.NotFound(
                    $"Package '{packageName}' version '{resolvedVersion}' was not found on nuget.org."
                );
            }

            if (!nuspecResponse.IsSuccessStatusCode)
            {
                return NuGetPackageMetadataLookup.FromProblem(
                    NuGetProblemDetailsResult.ServiceUnavailable(
                        $"nuget.org returned {(int)nuspecResponse.StatusCode} while reading package '{packageName}' version '{resolvedVersion}'."
                    )
                );
            }

            NuGetPackageMetadata? metadata;
            using var nuspecStream = nuspecResponse.Content.ReadAsStream();
            metadata = parser.Parse(nuspecStream);

            return metadata is null
                ? NuGetPackageMetadataLookup.FromProblem(
                    NuGetProblemDetailsResult.ServiceUnavailable(
                        $"nuget.org returned invalid metadata for package '{packageName}' version '{resolvedVersion}'."
                    )
                )
                : NuGetPackageMetadataLookup.Found(metadata);
        }
        catch (HttpRequestException exception)
        {
            return NuGetPackageMetadataLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable(
                    $"nuget.org could not be reached while looking up package '{packageName}': {exception.Message}"
                )
            );
        }
        catch (IOException exception)
        {
            return NuGetPackageMetadataLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable(
                    $"A network I/O error occurred while reading package '{packageName}' from nuget.org: {exception.Message}"
                )
            );
        }
        catch (JsonException exception)
        {
            return NuGetPackageMetadataLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable(
                    $"nuget.org returned invalid JSON while looking up package '{packageName}': {exception.Message}"
                )
            );
        }
    }

    public NuGetPackageVersionsLookup GetNuGetPackageVersions(
        string packageName,
        int? minimumMajor = null,
        bool includePreRelease = false,
        int? maxItems = null
    )
    {
        if (minimumMajor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumMajor),
                minimumMajor,
                "Minimum major must be non-negative."
            );
        }

        if (maxItems < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItems), maxItems, "Maximum items must be positive.");
        }

        var packageId = NormalizePackageId(packageName);

        try
        {
            var packageBaseAddress = GetPackageBaseAddress();
            if (packageBaseAddress is null)
            {
                return NuGetPackageVersionsLookup.FromProblem(
                    NuGetProblemDetailsResult.ServiceUnavailable(
                        "nuget.org did not advertise a package content endpoint."
                    )
                );
            }

            using var versionsResponse = Send(packageBaseAddress, $"{packageId}/index.json");
            if (versionsResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return NuGetPackageVersionsLookup.NotFound($"Package '{packageName}' was not found on nuget.org.");
            }

            if (!versionsResponse.IsSuccessStatusCode)
            {
                return NuGetPackageVersionsLookup.FromProblem(
                    NuGetProblemDetailsResult.ServiceUnavailable(
                        $"nuget.org returned {(int)versionsResponse.StatusCode} while looking up package '{packageName}' versions."
                    )
                );
            }

            using var versionsStream = versionsResponse.Content.ReadAsStream();
            using var versionsDocument = JsonDocument.Parse(versionsStream);
            var versions = GetVersions(versionsDocument.RootElement);

            if (versions is null)
            {
                return NuGetPackageVersionsLookup.FromProblem(
                    NuGetProblemDetailsResult.ServiceUnavailable(
                        $"nuget.org returned an invalid versions response for package '{packageName}'."
                    )
                );
            }

            var validVersions = versions
                .Select(version => new { Version = version, Parsed = NuGetPackageVersion.TryParse(version) })
                .Where(version =>
                    version.Parsed is not null
                    && (includePreRelease || version.Parsed.Prerelease is null)
                    && (minimumMajor is null || version.Parsed.Release[0] >= minimumMajor.Value)
                )
                .GroupBy(version => version.Version, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderByDescending(version => version.Parsed)
                .Select(version => version.Version);

            if (maxItems is not null)
            {
                validVersions = validVersions.Take(maxItems.Value);
            }

            var limitedVersions = validVersions.ToArray();

            return limitedVersions.Length == 0
                ? NuGetPackageVersionsLookup.NotFound(GetVersionsNotFoundDetail(packageName, minimumMajor))
                : NuGetPackageVersionsLookup.Found(limitedVersions);
        }
        catch (HttpRequestException exception)
        {
            return NuGetPackageVersionsLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable(
                    $"nuget.org could not be reached while looking up package '{packageName}' versions: {exception.Message}"
                )
            );
        }
        catch (IOException exception)
        {
            return NuGetPackageVersionsLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable(
                    $"A network I/O error occurred while reading package '{packageName}' versions from nuget.org: {exception.Message}"
                )
            );
        }
        catch (JsonException exception)
        {
            return NuGetPackageVersionsLookup.FromProblem(
                NuGetProblemDetailsResult.ServiceUnavailable(
                    $"nuget.org returned invalid JSON while looking up package '{packageName}' versions: {exception.Message}"
                )
            );
        }
    }

    private static string NormalizePackageId(string packageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var normalizedPackageName = packageName.Trim();
        if (
            normalizedPackageName.Contains("://", StringComparison.Ordinal)
            || normalizedPackageName.StartsWith('/')
            || normalizedPackageName.StartsWith('\\')
            || Uri.TryCreate(normalizedPackageName, UriKind.Absolute, out _)
        )
        {
            throw new ArgumentException("Package name must be a relative NuGet package ID.", nameof(packageName));
        }

        if (
            normalizedPackageName.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '-' and not '_'
            )
        )
        {
            throw new ArgumentException(
                "Package name contains characters that are not valid in a NuGet package ID.",
                nameof(packageName)
            );
        }

        return normalizedPackageName.ToLowerInvariant();
    }

    private Uri? GetPackageBaseAddress()
    {
        using var response = Send(ServiceIndexUri);
        response.EnsureSuccessStatusCode();
        using var stream = response.Content.ReadAsStream();
        using var document = JsonDocument.Parse(stream);

        if (
            !document.RootElement.TryGetProperty("resources", out var resources)
            || resources.ValueKind != JsonValueKind.Array
        )
        {
            return null;
        }

        foreach (var resource in resources.EnumerateArray())
        {
            if (
                !resource.TryGetProperty("@type", out var type)
                || !string.Equals(type.GetString(), "PackageBaseAddress/3.0.0", StringComparison.OrdinalIgnoreCase)
                || !resource.TryGetProperty("@id", out var id)
                || string.IsNullOrWhiteSpace(id.GetString())
            )
            {
                continue;
            }

            return new Uri(id.GetString()!, UriKind.Absolute);
        }

        return null;
    }

    private HttpResponseMessage Send(Uri requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        return httpClient.SendAsync(request).GetAwaiter().GetResult();
    }

    private HttpResponseMessage Send(Uri baseAddress, string relativePath)
    {
        return Send(new Uri(baseAddress, relativePath));
    }

    private static string[]? GetVersions(JsonElement rootElement)
    {
        if (
            !rootElement.TryGetProperty("versions", out var versionsElement)
            || versionsElement.ValueKind != JsonValueKind.Array
        )
        {
            return null;
        }

        return versionsElement
            .EnumerateArray()
            .Where(version => version.ValueKind == JsonValueKind.String)
            .Select(version => version.GetString())
            .OfType<string>()
            .ToArray();
    }

    private static string? ResolveVersion(IReadOnlyList<string> versions, string? requestedVersion)
    {
        if (!string.IsNullOrWhiteSpace(requestedVersion))
        {
            return versions.FirstOrDefault(version =>
                string.Equals(version, requestedVersion, StringComparison.OrdinalIgnoreCase)
            );
        }

        return versions
            .Select(version => new { Version = version, Parsed = NuGetPackageVersion.TryParse(version) })
            .Where(version => version.Parsed is not null)
            .OrderByDescending(version => version.Parsed)
            .FirstOrDefault()
            ?.Version;
    }

    private static string GetVersionsNotFoundDetail(string packageName, int? minimumMajor)
    {
        return minimumMajor is null
            ? $"Package '{packageName}' versions were not found on nuget.org."
            : $"Package '{packageName}' versions with major version >= {minimumMajor.Value} were not found on nuget.org.";
    }
}
