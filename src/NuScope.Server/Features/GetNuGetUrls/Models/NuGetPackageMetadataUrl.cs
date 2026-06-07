namespace Raiqub.NuSpec.Features.GetNuGetUrls.Models;

public sealed record NuGetPackageMetadataUrl
{
    public required string Source { get; init; }

    public required string Url { get; init; }
}
