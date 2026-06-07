using System.Net;
using System.Text.Json;
using Raiqub.NuSpec.Features.Common.Models;

namespace Raiqub.NuSpec.Features.Common.Services;

public sealed class NuGetOrgPackageMetadataClient(HttpClient httpClient, INuGetPackageMetadataParser parser)
    : INuGetRemotePackageMetadataClient
{
    private static readonly Uri ServiceIndexUri = new("https://api.nuget.org/v3/index.json");

    public NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

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

            var packageId = packageName.ToLowerInvariant();
            var versionsResponse = Send(packageBaseAddress, $"{packageId}/index.json");
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
            using (versionsResponse)
            {
                using var versionsStream = versionsResponse.Content.ReadAsStream();
                using var versionsDocument = JsonDocument.Parse(versionsStream);
                versions = GetVersions(versionsDocument.RootElement);
            }

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

            var nuspecResponse = Send(
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
            using (nuspecResponse)
            {
                using var nuspecStream = nuspecResponse.Content.ReadAsStream();
                metadata = parser.Parse(nuspecStream);
            }

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

        return versionsElement.EnumerateArray().Select(version => version.GetString()).OfType<string>().ToArray();
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
}
