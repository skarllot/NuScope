using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Raiqub.NuScope.Features.Common.Services;

namespace Raiqub.NuScope.Features.GetTypeApi.Tools;

[McpServerToolType]
public sealed class DllGetTypeApiTool(IDllMetadataService metadataService)
{
    [McpServerTool(
        Name = "dll_get_type_api",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false
    )]
    [Description("Returns a C# API declaration for a type from a local DLL without loading or executing the assembly.")]
    public CallToolResult GetTypeApi(
        [Description("The local DLL file path. Relative paths resolve against the server's working directory.")]
            string dllPath,
        [Description("The metadata full name of the type, including its namespace and generic arity.")]
            string fullTypeName,
        [Description("Whether to include private and internal members in addition to the public API.")]
            bool includePrivate = false
    )
    {
        var result = metadataService.GetTypeApi(dllPath, fullTypeName, includePrivate);
        if (result.Problem is not null)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = result.Problem.Detail }],
                StructuredContent = JsonSerializer.SerializeToElement(result.Problem),
            };
        }

        var typeApi = result.Result!;
        return new CallToolResult
        {
            Content =
            [
                new EmbeddedResourceBlock
                {
                    Resource = new TextResourceContents
                    {
                        Uri =
                            $"dll://assemblies/{Uri.EscapeDataString(dllPath)}/{Uri.EscapeDataString(fullTypeName)}.cs",
                        MimeType = "text/x-csharp",
                        Text = $"// Assembly: {typeApi.Assembly}{Environment.NewLine}{typeApi.Api}",
                    },
                },
            ],
        };
    }
}
