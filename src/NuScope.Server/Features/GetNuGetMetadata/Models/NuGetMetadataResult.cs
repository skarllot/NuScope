using Raiqub.NuSpec.Features.Common.Models;

namespace Raiqub.NuSpec.Features.GetNuGetMetadata.Models;

public sealed record NuGetMetadataResult : NuGetToolResult
{
    public required string Id { get; init; }

    public required string Version { get; init; }

    public string? Title { get; init; }

    public string? Authors { get; init; }

    public string? Owners { get; init; }

    public string? Description { get; init; }

    public string? Summary { get; init; }

    public string? ReleaseNotes { get; init; }

    public string? Language { get; init; }

    public string? ProjectUrl { get; init; }

    public string? IconUrl { get; init; }

    public string? Icon { get; init; }

    public string? LicenseUrl { get; init; }

    public string? LicenseType { get; init; }

    public string? License { get; init; }

    public NuGetMetadataRepository? Repository { get; init; }

    public bool RequireLicenseAcceptance { get; init; }

    public string? Copyright { get; init; }

    public string? Readme { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<NuGetMetadataDependencyGroup> DependencyGroups { get; init; } = [];

    public static NuGetMetadataResult FromMetadata(NuGetPackageMetadata metadata) =>
        new()
        {
            Id = metadata.Id,
            Version = metadata.Version,
            Title = metadata.Title,
            Authors = metadata.Authors,
            Owners = metadata.Owners,
            Description = metadata.Description,
            Summary = metadata.Summary,
            ReleaseNotes = metadata.ReleaseNotes,
            Language = metadata.Language,
            ProjectUrl = metadata.ProjectUrl,
            IconUrl = metadata.IconUrl,
            Icon = metadata.Icon,
            LicenseUrl = metadata.LicenseUrl,
            LicenseType = metadata.LicenseType,
            License = metadata.License,
            Repository = metadata.Repository is null
                ? null
                : NuGetMetadataRepository.FromRepository(metadata.Repository),
            RequireLicenseAcceptance = metadata.RequireLicenseAcceptance,
            Copyright = metadata.Copyright,
            Readme = metadata.Readme,
            Tags = metadata.Tags,
            DependencyGroups = metadata
                .DependencyGroups.Select(NuGetMetadataDependencyGroup.FromDependencyGroup)
                .ToArray(),
        };
}
