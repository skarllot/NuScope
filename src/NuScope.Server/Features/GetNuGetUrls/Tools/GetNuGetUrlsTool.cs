using System.ComponentModel;
using ModelContextProtocol.Server;
using Raiqub.NuSpec.Features.Common.Models;
using Raiqub.NuSpec.Features.Common.Services;
using Raiqub.NuSpec.Features.GetNuGetUrls.Models;
using Raiqub.NuSpec.Features.GetNuGetUrls.Services;

namespace Raiqub.NuSpec.Features.GetNuGetUrls.Tools;

[McpServerToolType]
public sealed class GetNuGetUrlsTool(INuGetPackageMetadataService metadataService)
{
    [McpServerTool(Name = "get_nuget_urls", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description(
        "Reads URLs from local NuGet package metadata, including project URL, repository URL, and any other URLs found."
    )]
    public NuGetToolResult GetNuGetUrls(
        [Description("The NuGet package id, for example 'Newtonsoft.Json'.")] string packageName,
        [Description(
            "The exact package version, for example '13.0.3'. If omitted, the latest version found in the local NuGet cache is used."
        )]
            string? version = null
    )
    {
        var metadataResult = metadataService.GetNuGetPackageMetadata(packageName, version);
        if (metadataResult.Problem is not null)
        {
            return metadataResult.Problem;
        }

        return NuGetPackageUrlExtractor.Extract(metadataResult.Metadata!);
    }
}
