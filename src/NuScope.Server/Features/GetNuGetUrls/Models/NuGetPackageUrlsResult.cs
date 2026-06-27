using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.GetNuGetUrls.Models;

public sealed record NuGetPackageUrlsResult : NuGetToolResult
{
    public required string Id { get; init; }

    public required string Version { get; init; }

    public string? ProjectUrl { get; init; }

    public string? RepositoryUrl { get; init; }

    public IReadOnlyList<NuGetPackageMetadataUrl> OtherUrls { get; init; } = [];

    public static NuGetPackageUrlsResult Found(
        string id,
        string version,
        string? projectUrl,
        string? repositoryUrl,
        IReadOnlyList<NuGetPackageMetadataUrl> otherUrls
    ) =>
        new()
        {
            Id = id,
            Version = version,
            ProjectUrl = projectUrl,
            RepositoryUrl = repositoryUrl,
            OtherUrls = otherUrls,
        };
}
