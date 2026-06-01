using System.ComponentModel;
using ModelContextProtocol.Server;
using Raiqub.NuSpec.Models;
using Raiqub.NuSpec.Services;

namespace Raiqub.NuSpec.Tools;

[McpServerToolType]
public sealed class NuGetPackageMetadataTools(INuGetPackageMetadataService metadataService)
{
    [McpServerTool]
    [Description("Reads package metadata from the local NuGet cache (~/.nuget/packages) for a package id and version.")]
    public NuGetPackageMetadataResult GetNuGetPackageMetadata(
        [Description("The NuGet package id, for example 'Newtonsoft.Json'.")] string packageName,
        [Description("The exact package version, for example '13.0.3'.")] string version
    ) => metadataService.GetNuGetPackageMetadata(packageName, version);
}
