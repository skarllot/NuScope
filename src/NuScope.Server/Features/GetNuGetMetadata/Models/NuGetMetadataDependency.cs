using CommonDependency = Raiqub.NuSpec.Features.Common.Models.NuGetDependency;

namespace Raiqub.NuSpec.Features.GetNuGetMetadata.Models;

public sealed record NuGetMetadataDependency
{
    public string? Id { get; init; }

    public string? Version { get; init; }

    public string? Exclude { get; init; }

    public static NuGetMetadataDependency FromDependency(CommonDependency dependency) =>
        new()
        {
            Id = dependency.Id,
            Version = dependency.Version,
            Exclude = dependency.Exclude,
        };
}
