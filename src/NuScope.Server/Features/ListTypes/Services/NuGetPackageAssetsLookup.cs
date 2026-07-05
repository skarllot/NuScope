using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.ListTypes.Services;

public sealed record NuGetPackageAssetsLookup
{
    public IReadOnlyList<NuGetPackageAsset>? Assets { get; init; }

    public NuGetProblemDetailsResult? Problem { get; init; }

    public NuGetPackageAssetSource Source { get; init; }

    public static NuGetPackageAssetsLookup Found(
        IReadOnlyList<NuGetPackageAsset> assets,
        NuGetPackageAssetSource source
    ) => new() { Assets = assets, Source = source };

    public static NuGetPackageAssetsLookup FromProblem(
        NuGetProblemDetailsResult problem,
        NuGetPackageAssetSource source
    ) => new() { Problem = problem, Source = source };
}
