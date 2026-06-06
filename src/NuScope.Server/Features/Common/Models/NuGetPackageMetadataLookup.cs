namespace Raiqub.NuSpec.Features.Common.Models;

public sealed record NuGetPackageMetadataLookup
{
    public NuGetPackageMetadata? Metadata { get; init; }

    public NuGetProblemDetailsResult? Problem { get; init; }

    public static NuGetPackageMetadataLookup Found(NuGetPackageMetadata metadata) => new() { Metadata = metadata };

    public static NuGetPackageMetadataLookup NotFound(string detail) =>
        new() { Problem = NuGetProblemDetailsResult.NotFound(detail) };
}
