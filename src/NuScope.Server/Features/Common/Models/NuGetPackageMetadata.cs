namespace Raiqub.NuSpec.Features.Common.Models;

public sealed record NuGetPackageMetadata
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

    public NuGetRepositoryMetadata? Repository { get; init; }

    public bool RequireLicenseAcceptance { get; init; }

    public string? Copyright { get; init; }

    public string? Readme { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<NuGetDependencyGroup> DependencyGroups { get; init; } = [];
}
