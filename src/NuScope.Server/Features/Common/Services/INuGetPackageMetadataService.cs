using Raiqub.NuSpec.Features.Common.Models;

namespace Raiqub.NuSpec.Features.Common.Services;

public interface INuGetPackageMetadataService
{
    NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null);
}
