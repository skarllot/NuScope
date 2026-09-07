using System.Globalization;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;

internal static class MetadataTypeNames
{
    public static string GetFullName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        var name = reader.GetString(type.Name);
        if (type.IsNested)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{GetFullName(reader, type.GetDeclaringType())}+{name}"
            );
        }

        var @namespace = reader.GetString(type.Namespace);
        return string.IsNullOrEmpty(@namespace)
            ? name
            : string.Create(CultureInfo.InvariantCulture, $"{@namespace}.{name}");
    }

    public static string FormatMetadataIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return identifier;
        }

        var arityIndex = identifier.IndexOf('`');
        if (arityIndex >= 0)
        {
            var unqualifiedName = identifier[..arityIndex];
            var aritySuffix = identifier[arityIndex..];
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{FormatMetadataIdentifier(unqualifiedName)}{aritySuffix}"
            );
        }

        return
            SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
            || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? string.Create(CultureInfo.InvariantCulture, $"@{identifier}")
            : identifier;
    }

    public static string FormatQualifiedMetadataName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var segments = name.Split('.');
        for (var index = 0; index < segments.Length; index++)
        {
            segments[index] = FormatMetadataIdentifier(segments[index]);
        }

        return string.Join(".", segments);
    }
}
