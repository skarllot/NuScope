using Raiqub.NuScope.Features.ListTypes.Models;

namespace Raiqub.NuScope.Features.ListTypes.Services;

public interface INuGetPackageTypeListingService
{
    NuGetPackageTypesLookup ListTypes(
        string packageName,
        string version,
        string targetFramework,
        string? filterRegex = null,
        bool includePrivate = false,
        bool includeExported = false
    );
}
