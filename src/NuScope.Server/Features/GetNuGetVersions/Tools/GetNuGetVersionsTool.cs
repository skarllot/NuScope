using System.ComponentModel;
using ModelContextProtocol.Server;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.GetNuGetVersions.Models;

namespace Raiqub.NuScope.Features.GetNuGetVersions.Tools;

[McpServerToolType]
public sealed class GetNuGetVersionsTool(INuGetPackageMetadataService metadataService)
{
    [McpServerTool(
        Name = "get_nuget_versions",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = true
    )]
    [Description("Reads available NuGet package versions from the local cache and nuget.org.")]
    public NuGetToolResult GetNuGetVersions(
        [Description("The NuGet package id, for example 'Newtonsoft.Json'.")] string packageName,
        [Description("The inclusive minimum major version to return, for example 8.")] int? minimumMajor = null,
        [Description("Whether to include prerelease versions such as '1.0.0-beta'.")] bool includePreRelease = false,
        [Description(
            "The maximum number of versions to return. If omitted, the latest 5 matching versions are returned."
        )]
            int? maxItems = 5,
        [Description(
            "Dependency detail to include: 'targetFrameworks' for dependency group target frameworks only, or 'full' for target frameworks and dependencies."
        )]
            NuGetVersionsIncludeDependency? includeDependency = null
    )
    {
        var result = metadataService.GetNuGetPackageVersions(packageName, minimumMajor, includePreRelease, maxItems);
        return result.Problem is not null
            ? result.Problem
            : CreateVersionsResult(packageName, result.Versions!, includeDependency);
    }

    private NuGetToolResult CreateVersionsResult(
        string packageName,
        IReadOnlyList<string> versions,
        NuGetVersionsIncludeDependency? includeDependency
    )
    {
        if (includeDependency is null)
        {
            return NuGetVersionsResult.Create([.. versions.Select(CreateVersionItem)]);
        }

        var items = new NuGetVersionItem[versions.Count];
        for (var index = 0; index < versions.Count; index++)
        {
            var version = versions[index];
            var metadataResult = metadataService.GetNuGetPackageMetadata(packageName, version);
            if (metadataResult.Problem is not null)
            {
                return metadataResult.Problem;
            }

            items[index] = new NuGetVersionItem
            {
                Version = version,
                DependencyGroups = GetDependencyGroups(
                    metadataResult.Metadata!.DependencyGroups,
                    includeDependency.Value
                ),
            };
        }

        return NuGetVersionsResult.Create(items);
    }

    private static NuGetVersionItem CreateVersionItem(string version) => new() { Version = version };

    private static NuGetVersionDependencyGroup[]? GetDependencyGroups(
        IReadOnlyList<NuGetDependencyGroup> dependencyGroups,
        NuGetVersionsIncludeDependency includeDependency
    )
    {
        if (dependencyGroups.Count == 0)
        {
            return null;
        }

        return includeDependency is NuGetVersionsIncludeDependency.TargetFrameworks
            ? [.. dependencyGroups.Select(NuGetVersionDependencyGroup.FromTargetFramework)]
            : [.. dependencyGroups.Select(NuGetVersionDependencyGroup.FromDependencyGroup)];
    }
}
