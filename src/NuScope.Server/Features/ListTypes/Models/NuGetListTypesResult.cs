using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.ListTypes.Models;

public sealed record NuGetListTypesResult : NuGetToolResult
{
    public required IReadOnlyList<NuGetTypeAssemblyResult> Assemblies { get; init; }

    public static NuGetListTypesResult Create(IReadOnlyList<NuGetTypeAssemblyResult> assemblies) =>
        new() { Assemblies = assemblies };
}
