using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.GetNuGetUrls.Models;
using Raiqub.NuScope.Features.GetNuGetUrls.Services;

namespace Raiqub.NuScope.Features.GetNuGetUrls.Tools;

[McpServerToolType]
public sealed class GetNuGetUrlsTool(INuGetPackageMetadataService metadataService)
{
    [McpServerTool(Name = "get_nuget_urls", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description(
        "Reads URLs from NuGet package metadata, including project URL, repository URL, and any other URLs found."
    )]
    public CallToolResult GetNuGetUrls(
        [Description("The NuGet package id, for example 'Newtonsoft.Json'.")] string packageName,
        [Description(
            "The exact package version, for example '13.0.3'. If omitted, the latest available version is used."
        )]
            string? version = null
    )
    {
        var metadataResult = metadataService.GetNuGetPackageMetadata(packageName, version);
        if (metadataResult.Problem is not null)
        {
            return metadataResult.Problem.ToCallToolResult();
        }

        NuGetToolResult toolResult = NuGetPackageUrlExtractor.Extract(metadataResult.Metadata!);
        return toolResult.ToCallToolResult();
    }
}
