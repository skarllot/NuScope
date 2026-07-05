using System.Text.Json.Serialization;

namespace Raiqub.NuScope.Features.GetNuGetVersions.Models;

public sealed record NuGetVersionItem
{
    public required string Version { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<NuGetVersionDependencyGroup>? DependencyGroups { get; init; }
}
