using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Raiqub.NuScope.Features.Common.Extensions;

public static class PortableToolSchemaExtensions
{
    public static IMcpServerBuilder WithPortableToolSchemas(this IMcpServerBuilder builder)
    {
        builder.Services.PostConfigure<McpServerOptions>(options =>
        {
            if (options.ToolCollection is null)
            {
                return;
            }

            foreach (var tool in options.ToolCollection)
            {
                tool.ProtocolTool.InputSchema = Normalize(tool.ProtocolTool.InputSchema);
            }
        });
        return builder;
    }

    internal static JsonElement Normalize(JsonElement schema)
    {
        var node = JsonNode.Parse(schema.GetRawText())!;
        NormalizeNode(node);
        return JsonSerializer.SerializeToElement(node);
    }

    private static void NormalizeNode(JsonNode? node)
    {
        if (node is not JsonObject schema)
        {
            return;
        }

        // Only visit schema-valued keywords; defaults, enums, and examples are literal data.
        foreach (var keyword in new[] { "properties", "patternProperties", "$defs", "definitions", "dependentSchemas" })
        {
            if (schema[keyword] is JsonObject children)
            {
                foreach (var child in children)
                {
                    NormalizeNode(child.Value);
                }
            }
        }

        foreach (var keyword in new[] { "anyOf", "allOf", "oneOf", "prefixItems" })
        {
            if (schema[keyword] is JsonArray children)
            {
                foreach (var child in children)
                {
                    NormalizeNode(child);
                }
            }
        }

        foreach (
            var keyword in new[]
            {
                "items",
                "additionalProperties",
                "unevaluatedProperties",
                "unevaluatedItems",
                "contains",
                "propertyNames",
                "not",
                "if",
                "then",
                "else",
            }
        )
        {
            NormalizeNode(schema[keyword]);
        }

        if (schema["type"] is not JsonArray types)
        {
            return;
        }

        var alternatives = new JsonArray();
        foreach (var type in types)
        {
            alternatives.Add(new JsonObject { ["type"] = type!.DeepClone() });
        }

        schema.Remove("type");
        if (schema.ContainsKey("anyOf"))
        {
            // Preserve an existing union as an independent constraint.
            if (schema["allOf"] is not JsonArray allOf)
            {
                allOf = new JsonArray();
                schema["allOf"] = allOf;
            }

            allOf.Add(new JsonObject { ["anyOf"] = alternatives });
        }
        else
        {
            schema["anyOf"] = alternatives;
        }
    }
}
