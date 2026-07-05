using System.ComponentModel;
using ModelContextProtocol.Server;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.ListTypes.Models;
using Raiqub.NuScope.Features.ListTypes.Services;

namespace Raiqub.NuScope.Features.ListTypes.Tools;

[McpServerToolType]
public sealed class NuGetListTypesTool(INuGetPackageTypeListingService typeListingService)
{
    [McpServerTool(
        Name = "nuget_list_types",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = true
    )]
    [Description("Lists metadata-defined types from assemblies in a NuGet package lib asset folder.")]
    public NuGetToolResult ListTypes(
        [Description("The NuGet package id, for example 'Newtonsoft.Json'.")] string packageName,
        [Description("The exact package version, for example '13.0.3'.")] string version,
        [Description("The target framework to resolve compatible lib or ref assets for, for example 'net8.0'.")]
            string targetFramework,
        [Description("Optional regex applied to the full type name only.")] string? filterRegex = null,
        [Description("Whether to include non-public metadata type definitions.")] bool includePrivate = false,
        [Description("Whether to include forwarded/exported metadata types.")] bool includeExported = false
    )
    {
        var result = typeListingService.ListTypes(
            packageName,
            version,
            targetFramework,
            filterRegex,
            includePrivate,
            includeExported
        );
        return result.Problem is not null ? result.Problem : NuGetListTypesResult.Create(result.Assemblies!);
    }
}
