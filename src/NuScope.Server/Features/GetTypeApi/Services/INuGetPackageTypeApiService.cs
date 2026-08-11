using Raiqub.NuScope.Features.GetTypeApi.Models;

namespace Raiqub.NuScope.Features.GetTypeApi.Services;

public interface INuGetPackageTypeApiService
{
    NuGetTypeApiLookup GetTypeApi(
        string packageName,
        string version,
        string targetFramework,
        string fullTypeName,
        bool includePrivate = false
    );
}
