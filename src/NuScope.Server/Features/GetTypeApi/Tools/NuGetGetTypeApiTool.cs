using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Raiqub.NuScope.Features.GetTypeApi.Services;

namespace Raiqub.NuScope.Features.GetTypeApi.Tools;

[McpServerToolType]
public sealed class NuGetGetTypeApiTool(INuGetPackageTypeApiService typeApiService)
{
    [McpServerTool(
        Name = "nuget_get_type_api",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = true
    )]
    [Description("Returns a C# API declaration for a type from a NuGet package assembly.")]
    public EmbeddedResourceBlock GetTypeApi(
        [Description("The NuGet package id, for example 'Newtonsoft.Json'.")] string packageName,
        [Description("The exact package version, for example '13.0.3'.")] string version,
        [Description("The target framework to resolve compatible lib or ref assets for, for example 'net8.0'.")]
            string targetFramework,
        [Description("The metadata full name of the type, including its namespace and generic arity.")]
            string fullTypeName,
        [Description("Whether to include private and internal members in addition to the public API.")]
            bool includePrivate = false
    )
    {
        var result = typeApiService.GetTypeApi(packageName, version, targetFramework, fullTypeName, includePrivate);
        if (result.Problem is not null)
        {
            throw new McpException(result.Problem.Detail);
        }

        var typeApi = result.Result!;
        var resourceUri =
            $"nuget://packages/{Uri.EscapeDataString(packageName)}/{Uri.EscapeDataString(version)}/{Uri.EscapeDataString(targetFramework)}/{Uri.EscapeDataString(fullTypeName)}.cs";

        return new EmbeddedResourceBlock
        {
            Resource = new TextResourceContents
            {
                Uri = resourceUri,
                MimeType = "text/x-csharp",
                Text = $"// Assembly: {typeApi.Assembly}{Environment.NewLine}{typeApi.Api}",
            },
        };
    }
}
