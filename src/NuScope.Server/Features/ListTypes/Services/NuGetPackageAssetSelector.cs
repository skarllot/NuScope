using NuGet.Frameworks;

namespace Raiqub.NuScope.Features.ListTypes.Services;

public static class NuGetPackageAssetSelector
{
    public static readonly string[] AssetRoots = ["lib", "ref"];

    public static NuGetPackageAssetFolder[] SelectCompatibleAssetFolders(
        string targetFramework,
        IReadOnlyList<NuGetPackageAssetFolder> assets
    )
    {
        var target = NuGetFramework.ParseFolder(targetFramework);
        if (target.IsUnsupported)
        {
            return [];
        }

        return
        [
            .. assets
                .GroupBy(asset => asset.Root, StringComparer.OrdinalIgnoreCase)
                .Select(group => SelectCompatibleAssetFolder(target, group.ToArray()))
                .OfType<NuGetPackageAssetFolder>()
                .OrderBy(asset => asset.Root, NuGetPackageAssetRootComparer.Instance),
        ];
    }

    public static string DescribeAssetFolders(IReadOnlyList<NuGetPackageAssetFolder> assetFolders)
    {
        var descriptions = assetFolders.Select(assetFolder => $"'{assetFolder.Root}/{assetFolder.Name}'").ToArray();
        return descriptions.Length == 1 ? descriptions[0] : string.Join(" or ", descriptions);
    }

    private static NuGetPackageAssetFolder? SelectCompatibleAssetFolder(
        NuGetFramework target,
        IReadOnlyList<NuGetPackageAssetFolder> assets
    )
    {
        var candidates = assets
            .Select(asset => new { Asset = asset, Framework = NuGetFramework.ParseFolder(asset.Name) })
            .Where(asset => !asset.Framework.IsUnsupported)
            .ToArray();
        var nearest = new FrameworkReducer().GetNearest(target, candidates.Select(asset => asset.Framework));
        return nearest is null ? null : candidates.First(asset => asset.Framework.Equals(nearest)).Asset;
    }
}
