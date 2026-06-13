using Raiqub.NuSpec.Features.Common.Models;

namespace Raiqub.NuSpec.Features.Common.Services;

public interface INuGetRemotePackageMetadataClient
{
    NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null);
}
