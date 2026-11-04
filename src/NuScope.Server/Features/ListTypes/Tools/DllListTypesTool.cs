using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.ListTypes.Models;

namespace Raiqub.NuScope.Features.ListTypes.Tools;

[McpServerToolType]
public sealed class DllListTypesTool(IDllMetadataService metadataService)
{
    [McpServerTool(Name = "dll_list_types", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists metadata-defined types from a local DLL file without loading or executing the assembly.")]
    public CallToolResult ListTypes(
        [Description("The local DLL file path. Relative paths resolve against the server's working directory.")]
            string dllPath,
        [Description("Optional regex applied to the full type name only.")] string? filterRegex = null,
        [Description("Whether to include non-public metadata type definitions.")] bool includePrivate = false,
        [Description("Whether to include forwarded/exported metadata types.")] bool includeExported = false
    )
    {
        var result = metadataService.ListTypes(dllPath, filterRegex, includePrivate, includeExported);
        NuGetToolResult toolResult = result.Problem is not null
            ? result.Problem
            : NuGetListTypesResult.Create(result.Assemblies!);
        return toolResult.ToCallToolResult();
    }
}
