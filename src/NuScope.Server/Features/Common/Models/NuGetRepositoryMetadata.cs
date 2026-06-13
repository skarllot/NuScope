namespace Raiqub.NuScope.Features.Common.Models;

public sealed record NuGetRepositoryMetadata
{
    public string? Type { get; init; }

    public string? Url { get; init; }

    public string? Branch { get; init; }

    public string? Commit { get; init; }
}
