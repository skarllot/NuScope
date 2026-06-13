using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.Common.Services;

public interface INuGetPackageMetadataService
{
    NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null);
}
