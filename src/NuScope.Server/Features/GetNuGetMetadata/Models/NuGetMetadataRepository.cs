using CommonRepository = Raiqub.NuScope.Features.Common.Models.NuGetRepositoryMetadata;

namespace Raiqub.NuScope.Features.GetNuGetMetadata.Models;

public sealed record NuGetMetadataRepository
{
    public string? Type { get; init; }

    public string? Url { get; init; }

    public string? Branch { get; init; }

    public string? Commit { get; init; }

    public static NuGetMetadataRepository FromRepository(CommonRepository repository) =>
        new()
        {
            Type = repository.Type,
            Url = repository.Url,
            Branch = repository.Branch,
            Commit = repository.Commit,
        };
}
