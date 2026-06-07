using CommonDependencyGroup = Raiqub.NuSpec.Features.Common.Models.NuGetDependencyGroup;

namespace Raiqub.NuSpec.Features.GetNuGetMetadata.Models;

public sealed record NuGetMetadataDependencyGroup
{
    public string? TargetFramework { get; init; }

    public IReadOnlyList<NuGetMetadataDependency> Dependencies { get; init; } = [];

    public static NuGetMetadataDependencyGroup FromDependencyGroup(CommonDependencyGroup dependencyGroup) =>
        new()
        {
            TargetFramework = dependencyGroup.TargetFramework,
            Dependencies = dependencyGroup.Dependencies.Select(NuGetMetadataDependency.FromDependency).ToArray(),
        };
}
