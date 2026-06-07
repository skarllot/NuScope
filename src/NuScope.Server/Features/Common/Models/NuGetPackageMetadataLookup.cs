namespace Raiqub.NuSpec.Features.Common.Models;

public sealed record NuGetPackageMetadataLookup
{
    private NuGetPackageMetadataLookup(NuGetPackageMetadata? metadata, NuGetProblemDetailsResult? problem)
    {
        Metadata = metadata;
        Problem = problem;
    }

    public NuGetPackageMetadata? Metadata { get; }

    public NuGetProblemDetailsResult? Problem { get; }

    public static NuGetPackageMetadataLookup Found(NuGetPackageMetadata metadata) => new(metadata, null);

    public static NuGetPackageMetadataLookup FromProblem(NuGetProblemDetailsResult problem) => new(null, problem);

    public static NuGetPackageMetadataLookup NotFound(string detail) =>
        new(null, NuGetProblemDetailsResult.NotFound(detail));
}
