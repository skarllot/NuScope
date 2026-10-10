using Raiqub.NuScope.Features.GetTypeApi.Models;
using Raiqub.NuScope.Features.ListTypes.Models;

namespace Raiqub.NuScope.Features.Common.Services;

public interface IDllMetadataService
{
    NuGetPackageTypesLookup ListTypes(
        string dllPath,
        string? filterRegex = null,
        bool includePrivate = false,
        bool includeExported = false
    );

    NuGetTypeApiLookup GetTypeApi(string dllPath, string fullTypeName, bool includePrivate = false);
}
