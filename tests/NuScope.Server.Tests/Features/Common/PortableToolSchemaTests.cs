using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Raiqub.NuScope.Features.Common.Extensions;
using Raiqub.NuScope.Features.GetNuGetVersions.Tools;
using Raiqub.NuScope.Features.ListTypes.Tools;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.Common;

public sealed class PortableToolSchemaTests
{
    [Fact]
    public void RegisteredToolsUseSingleTypeBranchesAndPreserveParameterContracts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddMcpServer()
            .WithTools<DllListTypesTool>()
            .WithTools<NuGetListTypesTool>()
            .WithTools<GetNuGetVersionsTool>()
            .WithPortableToolSchemas();
        using var provider = services.BuildServiceProvider();
        var tools = provider.GetRequiredService<IOptions<McpServerOptions>>().Value.ToolCollection!;

        foreach (var tool in tools)
        {
            var schema = tool.ProtocolTool.InputSchema;
            AssertSingleTypes(schema);
            Assert.Equal("object", schema.GetProperty("type").GetString());
            var properties = schema.GetProperty("properties");
            var required = schema.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToArray();
            if (tool.ProtocolTool.Name is "dll_list_types" or "nuget_list_types")
            {
                var filter = properties.GetProperty("filterRegex");
                Assert.Equal(
                    ["string", "null"],
                    filter
                        .GetProperty("anyOf")
                        .EnumerateArray()
                        .Select(branch => branch.GetProperty("type").GetString())
                );
                Assert.DoesNotContain("filterRegex", required);
                Assert.Equal(JsonValueKind.Null, filter.GetProperty("default").ValueKind);
                Assert.Contains("full type name", filter.GetProperty("description").GetString());
                Assert.Contains(tool.ProtocolTool.Name == "dll_list_types" ? "dllPath" : "packageName", required);
            }
            else
            {
                Assert.DoesNotContain("maxItems", required);
                Assert.Equal(5, properties.GetProperty("maxItems").GetProperty("default").GetInt32());
                Assert.Equal(
                    ["integer", "null"],
                    properties
                        .GetProperty("maxItems")
                        .GetProperty("anyOf")
                        .EnumerateArray()
                        .Select(branch => branch.GetProperty("type").GetString())
                );
                Assert.True(properties.GetProperty("includeDependency").TryGetProperty("anyOf", out _));
            }
        }
    }

    [Fact]
    public void NormalizationPreservesExistingUnionConstraintsAndLiteralData()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "type": "object",
              "properties": {
                "value": {
                  "type": ["string", "null"],
                  "anyOf": [{"const": "example"}, {"type": "null"}],
                  "default": {"type": ["literal", "data"]}
                },
                "values": {"type": "array", "items": {"type": ["integer", "null"]}}
              }
            }
            """
        );

        var normalized = PortableToolSchemaExtensions.Normalize(document.RootElement);
        var value = normalized.GetProperty("properties").GetProperty("value");
        Assert.False(value.TryGetProperty("type", out _));
        Assert.Equal("example", value.GetProperty("anyOf")[0].GetProperty("const").GetString());
        Assert.Equal(
            ["string", "null"],
            value
                .GetProperty("allOf")[0]
                .GetProperty("anyOf")
                .EnumerateArray()
                .Select(branch => branch.GetProperty("type").GetString())
        );
        Assert.Equal(
            ["literal", "data"],
            value.GetProperty("default").GetProperty("type").EnumerateArray().Select(item => item.GetString())
        );
        Assert.Equal(
            "integer",
            normalized
                .GetProperty("properties")
                .GetProperty("values")
                .GetProperty("items")
                .GetProperty("anyOf")[0]
                .GetProperty("type")
                .GetString()
        );
        Assert.Equal(normalized.GetRawText(), PortableToolSchemaExtensions.Normalize(normalized).GetRawText());
    }

    private static void AssertSingleTypes(JsonElement schema)
    {
        if (schema.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in schema.EnumerateObject())
            {
                if (property.Name == "type")
                {
                    Assert.Equal(JsonValueKind.String, property.Value.ValueKind);
                }
                else
                {
                    AssertSingleTypes(property.Value);
                }
            }
        }
        else if (schema.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in schema.EnumerateArray())
            {
                AssertSingleTypes(item);
            }
        }
    }
}
