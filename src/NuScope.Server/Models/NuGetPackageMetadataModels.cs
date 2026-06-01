namespace Raiqub.NuSpec.Models;

public sealed record NuGetPackageMetadataResult
{
    public required bool IsFound { get; init; }

    public required string PackageName { get; init; }

    public required string Version { get; init; }

    public string? PackageDirectory { get; init; }

    public string? NuspecPath { get; init; }

    public string? Message { get; init; }

    public NuGetPackageMetadata? Metadata { get; init; }

    public static NuGetPackageMetadataResult NotFound(string packageName, string version, string message) =>
        new()
        {
            IsFound = false,
            PackageName = packageName,
            Version = version,
            Message = message,
        };

    public static NuGetPackageMetadataResult Found(
        string packageName,
        string version,
        string packageDirectory,
        string nuspecPath,
        NuGetPackageMetadata metadata
    ) =>
        new()
        {
            IsFound = true,
            PackageName = packageName,
            Version = version,
            PackageDirectory = packageDirectory,
            NuspecPath = nuspecPath,
            Metadata = metadata,
        };
}

public sealed record NuGetPackageMetadata
{
    public string? Id { get; init; }

    public string? Version { get; init; }

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

    public bool RequireLicenseAcceptance { get; init; }

    public string? Copyright { get; init; }

    public string? Readme { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<NuGetDependencyGroup> DependencyGroups { get; init; } = [];
}

public sealed record NuGetDependencyGroup
{
    public string? TargetFramework { get; init; }

    public IReadOnlyList<NuGetDependency> Dependencies { get; init; } = [];
}

public sealed record NuGetDependency
{
    public string? Id { get; init; }

    public string? Version { get; init; }

    public string? Exclude { get; init; }
}
