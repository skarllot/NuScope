using System.Text.Json.Serialization;
using CommonDependencyGroup = Raiqub.NuScope.Features.Common.Models.NuGetDependencyGroup;

namespace Raiqub.NuScope.Features.GetNuGetVersions.Models;

public sealed record NuGetVersionDependencyGroup
{
    public string? TargetFramework { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Dependencies { get; init; }

    public static NuGetVersionDependencyGroup FromDependencyGroup(CommonDependencyGroup dependencyGroup) =>
        new() { TargetFramework = dependencyGroup.TargetFramework, Dependencies = GetDependencies(dependencyGroup) };

    public static NuGetVersionDependencyGroup FromTargetFramework(CommonDependencyGroup dependencyGroup) =>
        new() { TargetFramework = dependencyGroup.TargetFramework };

    private static string[]? GetDependencies(CommonDependencyGroup dependencyGroup)
    {
        var dependencies = dependencyGroup
            .Dependencies.Select(dependency =>
                string.Join(
                    ' ',
                    new[] { dependency.Id, dependency.Version }.Where(value => !string.IsNullOrWhiteSpace(value))
                )
            )
            .Where(dependency => dependency.Length > 0)
            .ToArray();

        return dependencies.Length == 0 ? null : dependencies;
    }
}
