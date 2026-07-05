namespace Raiqub.NuScope.Features.ListTypes.Services;

public sealed class NuGetPackageAssetRootComparer : IComparer<string>
{
    public static readonly NuGetPackageAssetRootComparer Instance = new();

    public int Compare(string? x, string? y) => GetOrder(x).CompareTo(GetOrder(y));

    private static int GetOrder(string? root)
    {
        return root?.ToLowerInvariant() switch
        {
            "lib" => 0,
            "ref" => 1,
            _ => 2,
        };
    }
}
