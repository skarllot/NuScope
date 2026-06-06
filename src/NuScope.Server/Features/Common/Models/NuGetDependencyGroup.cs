namespace Raiqub.NuSpec.Features.Common.Models;

public sealed record NuGetDependencyGroup
{
    public string? TargetFramework { get; init; }

    public IReadOnlyList<NuGetDependency> Dependencies { get; init; } = [];
}
