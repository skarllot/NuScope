namespace Raiqub.NuSpec.Features.Common.Models;

public sealed record NuGetDependency
{
    public string? Id { get; init; }

    public string? Version { get; init; }

    public string? Exclude { get; init; }
}
