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
        [Description("The maximum number of versions to return. If omitted, all matching versions are returned.")]
            int? maxItems = null
    )
    {
        var result = metadataService.GetNuGetPackageVersions(packageName, minimumMajor, includePreRelease, maxItems);
        return result.Problem is not null ? result.Problem : NuGetVersionsResult.Create([.. result.Versions!]);
    }
}
