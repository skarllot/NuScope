using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Raiqub.NuScope.Features.ListTypes.Models;

namespace Raiqub.NuScope.Features.ListTypes.Services;

public sealed class NuGetAssemblyTypeReader : INuGetAssemblyTypeReader
{
    public NuGetTypeAssemblyResult ReadTypes(
        string assemblyName,
        Stream stream,
        Regex? filter,
        bool includePrivate,
        bool includeExported
    )
    {
        using var bufferedStream = GetSeekableStream(stream);
        using var peReader = new PEReader(bufferedStream);
        if (!peReader.HasMetadata)
        {
            throw new BadImageFormatException($"Assembly '{assemblyName}' does not contain metadata.");
        }

        var metadataReader = peReader.GetMetadataReader();
        var types = metadataReader
            .TypeDefinitions.Select(handle => new { Handle = handle, Type = metadataReader.GetTypeDefinition(handle) })
            .Where(type => metadataReader.GetString(type.Type.Name) != "<Module>")
            .Where(type => includePrivate || IsPublic(type.Type.Attributes))
            .Select(type =>
            {
                var fullName = GetFullName(metadataReader, type.Handle, type.Type);
                return new
                {
                    FullName = fullName,
                    RenderedName = string.Create(
                        CultureInfo.InvariantCulture,
                        $"{GetKind(metadataReader, type.Type)} {fullName}"
                    ),
                };
            })
            .Where(type => filter is null || filter.IsMatch(type.FullName))
            .GroupBy(type => type.FullName, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => type.RenderedName)
            .ToArray();
        var exported = includeExported ? GetExportedTypes(metadataReader, filter) : [];

        return new NuGetTypeAssemblyResult
        {
            Assembly = assemblyName,
            Exported = exported,
            Types = types,
        };
    }

    private static string[] GetExportedTypes(MetadataReader metadataReader, Regex? filter)
    {
        return
        [
            .. metadataReader
                .ExportedTypes.Select(handle => metadataReader.GetExportedType(handle))
                .Select(type =>
                {
                    var fullName = GetFullName(metadataReader, type);
                    return new
                    {
                        FullName = fullName,
                        RenderedName = string.Create(CultureInfo.InvariantCulture, $"type {fullName}"),
                    };
                })
                .Where(type => filter is null || filter.IsMatch(type.FullName))
                .GroupBy(type => type.FullName, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(type => type.RenderedName),
        ];
    }

    private static Stream GetSeekableStream(Stream stream)
    {
        if (stream.CanSeek)
        {
            return stream;
        }

        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    private static bool IsPublic(TypeAttributes attributes)
    {
        var visibility = attributes & TypeAttributes.VisibilityMask;
        return visibility is TypeAttributes.Public or TypeAttributes.NestedPublic;
    }

    private static bool IsNested(TypeAttributes attributes)
    {
        var visibility = attributes & TypeAttributes.VisibilityMask;
        return visibility
            is TypeAttributes.NestedPublic
                or TypeAttributes.NestedPrivate
                or TypeAttributes.NestedFamily
                or TypeAttributes.NestedAssembly
                or TypeAttributes.NestedFamANDAssem
                or TypeAttributes.NestedFamORAssem;
    }

    private static string GetKind(MetadataReader metadataReader, TypeDefinition type)
    {
        if ((type.Attributes & TypeAttributes.Interface) == TypeAttributes.Interface)
        {
            return "interface";
        }

        return GetBaseTypeFullName(metadataReader, type.BaseType) switch
        {
            "System.Enum" => "enum",
            "System.MulticastDelegate" => "delegate",
            "System.ValueType" => "struct",
            _ => "class",
        };
    }

    private static string? GetBaseTypeFullName(MetadataReader metadataReader, EntityHandle handle)
    {
        return handle.Kind switch
        {
            HandleKind.TypeDefinition => GetFullName(
                metadataReader,
                (TypeDefinitionHandle)handle,
                metadataReader.GetTypeDefinition((TypeDefinitionHandle)handle)
            ),
            HandleKind.TypeReference => GetFullName(
                metadataReader,
                metadataReader.GetTypeReference((TypeReferenceHandle)handle)
            ),
            _ => null,
        };
    }

    private static string GetFullName(MetadataReader metadataReader, TypeDefinitionHandle handle, TypeDefinition type)
    {
        if (type.IsNested)
        {
            var declaringType = metadataReader.GetTypeDefinition(type.GetDeclaringType());
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{GetFullName(metadataReader, type.GetDeclaringType(), declaringType)}+{metadataReader.GetString(type.Name)}"
            );
        }

        var name = metadataReader.GetString(type.Name);
        var @namespace = metadataReader.GetString(type.Namespace);
        return string.IsNullOrEmpty(@namespace)
            ? name
            : string.Create(CultureInfo.InvariantCulture, $"{@namespace}.{name}");
    }

    private static string GetFullName(MetadataReader metadataReader, TypeDefinition type)
    {
        var name = metadataReader.GetString(type.Name);
        var @namespace = metadataReader.GetString(type.Namespace);
        return string.IsNullOrEmpty(@namespace)
            ? name
            : string.Create(CultureInfo.InvariantCulture, $"{@namespace}.{name}");
    }

    private static string GetFullName(MetadataReader metadataReader, TypeReference type)
    {
        var name = metadataReader.GetString(type.Name);
        var @namespace = metadataReader.GetString(type.Namespace);
        return string.IsNullOrEmpty(@namespace)
            ? name
            : string.Create(CultureInfo.InvariantCulture, $"{@namespace}.{name}");
    }

    private static string GetFullName(MetadataReader metadataReader, ExportedType type)
    {
        if (IsNested(type.Attributes))
        {
            var implementation = type.Implementation;
            if (implementation.Kind == HandleKind.ExportedType)
            {
                var declaringType = metadataReader.GetExportedType((ExportedTypeHandle)implementation);
                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"{GetFullName(metadataReader, declaringType)}+{metadataReader.GetString(type.Name)}"
                );
            }
        }

        var name = metadataReader.GetString(type.Name);
        var @namespace = metadataReader.GetString(type.Namespace);
        return string.IsNullOrEmpty(@namespace)
            ? name
            : string.Create(CultureInfo.InvariantCulture, $"{@namespace}.{name}");
    }
}
