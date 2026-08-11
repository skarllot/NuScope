using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.GetTypeApi.Models;
using Raiqub.NuScope.Features.ListTypes.Services;

namespace Raiqub.NuScope.Features.GetTypeApi.Services;

public sealed class NuGetPackageTypeApiService(
    INuGetPackageAssetResolver assetResolver,
    INuGetTypeApiReader typeApiReader
) : INuGetPackageTypeApiService
{
    public NuGetTypeApiLookup GetTypeApi(
        string packageName,
        string version,
        string targetFramework,
        string fullTypeName,
        bool includePrivate = false
    )
    {
        var validationProblem = ValidateInputs(packageName, version, targetFramework, fullTypeName);
        if (validationProblem is not null)
        {
            return NuGetTypeApiLookup.FromProblem(validationProblem);
        }

        var assetsLookup = assetResolver.GetAssets(packageName, version, targetFramework);
        if (assetsLookup.Problem is not null)
        {
            return NuGetTypeApiLookup.FromProblem(assetsLookup.Problem);
        }

        var readableAssemblyCount = 0;
        foreach (var asset in assetsLookup.Assets!)
        {
            try
            {
                using var stream = asset.OpenRead();
                var api = typeApiReader.ReadTypeApi(stream, fullTypeName, includePrivate);
                readableAssemblyCount++;
                if (api is not null)
                {
                    return NuGetTypeApiLookup.Found(asset.Label, api);
                }
            }
            catch (BadImageFormatException)
            {
                // A package may contain native or otherwise unreadable DLLs next to managed assemblies.
            }
        }

        if (readableAssemblyCount == 0)
        {
            return NuGetTypeApiLookup.FromProblem(
                assetsLookup.Source == NuGetPackageAssetSource.Remote
                    ? NuGetProblemDetailsResult.ServiceUnavailable(
                        $"nuget.org returned a package with no readable DLL metadata for "
                            + $"'{packageName}' version '{version}'."
                    )
                    : NuGetProblemDetailsResult.InternalServerError(
                        $"Package '{packageName}' version '{version}' contains no readable DLL metadata."
                    )
            );
        }

        return NuGetTypeApiLookup.FromProblem(
            NuGetProblemDetailsResult.NotFound(
                $"Type '{fullTypeName}' was not found in the compatible lib or ref assets for "
                    + $"package '{packageName}' version '{version}', or it is not public."
            )
        );
    }

    private static NuGetProblemDetailsResult? ValidateInputs(
        string packageName,
        string version,
        string targetFramework,
        string fullTypeName
    )
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return NuGetProblemDetailsResult.BadRequest("Package name is required.");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            return NuGetProblemDetailsResult.BadRequest("Package version is required.");
        }

        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            return NuGetProblemDetailsResult.BadRequest("Target framework is required.");
        }

        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return NuGetProblemDetailsResult.BadRequest("Full type name is required.");
        }

        try
        {
            _ = NuGetPackageId.Normalize(packageName);
        }
        catch (ArgumentException exception)
        {
            return NuGetProblemDetailsResult.BadRequest(exception.Message);
        }

        return null;
    }
}
