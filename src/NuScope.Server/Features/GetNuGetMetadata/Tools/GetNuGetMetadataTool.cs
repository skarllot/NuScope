using System.ComponentModel;
using ModelContextProtocol.Server;
using Raiqub.NuSpec.Features.Common.Models;
using Raiqub.NuSpec.Features.Common.Services;
using Raiqub.NuSpec.Features.GetNuGetMetadata.Models;

namespace Raiqub.NuSpec.Features.GetNuGetMetadata.Tools;

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
    [Description("Reads package metadata from the local NuGet cache (~/.nuget/packages) for a package id and version.")]
    public NuGetToolResult GetNuGetMetadata(
        [Description("The NuGet package id, for example 'Newtonsoft.Json'.")] string packageName,
        [Description(
            "The exact package version, for example '13.0.3'. If omitted, the latest version found in the local NuGet cache is used."
        )]
            string? version = null
    )
    {
        var result = metadataService.GetNuGetPackageMetadata(packageName, version);
        return result.Problem is not null ? result.Problem : NuGetMetadataResult.FromMetadata(result.Metadata!);
    }
}
