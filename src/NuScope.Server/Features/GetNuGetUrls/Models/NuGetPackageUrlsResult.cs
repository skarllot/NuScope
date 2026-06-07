using Raiqub.NuSpec.Features.Common.Models;

namespace Raiqub.NuSpec.Features.GetNuGetUrls.Models;

public sealed record NuGetPackageUrlsResult : NuGetToolResult
{
    public string? ProjectUrl { get; init; }

    public string? RepositoryUrl { get; init; }

    public IReadOnlyList<NuGetPackageMetadataUrl> OtherUrls { get; init; } = [];

    public static NuGetPackageUrlsResult Found(
        string? projectUrl,
        string? repositoryUrl,
        IReadOnlyList<NuGetPackageMetadataUrl> otherUrls
    ) =>
        new()
        {
            ProjectUrl = projectUrl,
            RepositoryUrl = repositoryUrl,
            OtherUrls = otherUrls,
        };
}
