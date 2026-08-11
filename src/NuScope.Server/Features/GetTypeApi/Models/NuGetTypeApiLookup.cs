using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.GetTypeApi.Models;

public sealed record NuGetTypeApiLookup
{
    public NuGetTypeApiResult? Result { get; init; }

    public NuGetProblemDetailsResult? Problem { get; init; }

    public static NuGetTypeApiLookup Found(string assembly, string api) =>
        new()
        {
            Result = new NuGetTypeApiResult { Assembly = assembly, Api = api },
        };

    public static NuGetTypeApiLookup FromProblem(NuGetProblemDetailsResult problem) => new() { Problem = problem };
}
