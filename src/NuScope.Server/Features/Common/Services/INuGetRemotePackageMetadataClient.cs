using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.Common.Services;

public interface INuGetRemotePackageMetadataClient
{
    NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null);

    NuGetPackageVersionsLookup GetNuGetPackageVersions(
        string packageName,
        int? minimumMajor = null,
        bool includePreRelease = false,
        int? maxItems = null
    );
}
