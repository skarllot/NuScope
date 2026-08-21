using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.GetNuGetVersions.Models;

public sealed record NuGetVersionsResult : NuGetToolResult
{
    public required IReadOnlyList<NuGetVersionItem> Versions { get; init; }

    public static NuGetVersionsResult Create(IReadOnlyList<NuGetVersionItem> versions) => new() { Versions = versions };
}
