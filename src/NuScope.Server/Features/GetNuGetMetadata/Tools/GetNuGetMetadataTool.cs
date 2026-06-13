using System.ComponentModel;
using ModelContextProtocol.Server;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.GetNuGetMetadata.Models;

namespace Raiqub.NuScope.Features.GetNuGetMetadata.Tools;

[McpServerToolType]
public sealed class GetNuGetMetadataTool(INuGetPackageMetadataService metadataService)
{
    [McpServerTool(
        Name = "get_nuget_metadata",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = true
    )]
    [Description("Reads package metadata for a package id and version.")]
    public NuGetToolResult GetNuGetMetadata(
        [Description("The NuGet package id, for example 'Newtonsoft.Json'.")] string packageName,
        [Description(
            "The exact package version, for example '13.0.3'. If omitted, the latest available version is used."
        )]
            string? version = null
    )
    {
        var result = metadataService.GetNuGetPackageMetadata(packageName, version);
        return result.Problem is not null ? result.Problem : NuGetMetadataResult.FromMetadata(result.Metadata!);
    }
}
