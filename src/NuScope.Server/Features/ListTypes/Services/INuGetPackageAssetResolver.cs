namespace Raiqub.NuScope.Features.ListTypes.Services;

public interface INuGetPackageAssetResolver
{
    NuGetPackageAssetsLookup GetAssets(string packageName, string version, string targetFramework);
}
