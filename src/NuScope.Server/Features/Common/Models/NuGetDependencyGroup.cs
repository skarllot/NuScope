namespace Raiqub.NuScope.Features.Common.Models;

public sealed record NuGetDependencyGroup
{
    public string? TargetFramework { get; init; }

    public IReadOnlyList<NuGetDependency> Dependencies { get; init; } = [];
}
