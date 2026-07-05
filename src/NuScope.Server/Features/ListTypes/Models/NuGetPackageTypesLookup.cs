using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.ListTypes.Models;

public sealed record NuGetPackageTypesLookup
{
    public IReadOnlyList<NuGetTypeAssemblyResult>? Assemblies { get; init; }

    public NuGetProblemDetailsResult? Problem { get; init; }

    public static NuGetPackageTypesLookup Found(IReadOnlyList<NuGetTypeAssemblyResult> assemblies) =>
        new() { Assemblies = assemblies };

    public static NuGetPackageTypesLookup FromProblem(NuGetProblemDetailsResult problem) => new() { Problem = problem };
}
