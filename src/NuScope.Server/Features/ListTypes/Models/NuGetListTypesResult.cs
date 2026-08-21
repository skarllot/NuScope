using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.ListTypes.Models;

public sealed record NuGetListTypesResult : NuGetToolCollectionResult<NuGetTypeAssemblyResult>
{
    private NuGetListTypesResult(IReadOnlyList<NuGetTypeAssemblyResult> assemblies)
        : base(assemblies) { }

    public static NuGetListTypesResult Create(IReadOnlyList<NuGetTypeAssemblyResult> assemblies) => new(assemblies);
}
