namespace Raiqub.NuScope.Features.Common.Models;

public sealed record NuGetPackageVersionsLookup
{
    private NuGetPackageVersionsLookup(IReadOnlyList<string>? versions, NuGetProblemDetailsResult? problem)
    {
        Versions = versions;
        Problem = problem;
    }

    public IReadOnlyList<string>? Versions { get; }

    public NuGetProblemDetailsResult? Problem { get; }

    public static NuGetPackageVersionsLookup Found(IReadOnlyList<string> versions) => new(versions, null);

    public static NuGetPackageVersionsLookup FromProblem(NuGetProblemDetailsResult problem) => new(null, problem);

    public static NuGetPackageVersionsLookup NotFound(string detail) =>
        new(null, NuGetProblemDetailsResult.NotFound(detail));
}
