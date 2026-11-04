using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;

internal sealed class AttributeEnumMetadataResolver(MetadataReader reader)
{
    private readonly SignatureTypeNameProvider names = new();
    private readonly Dictionary<string, EnumMetadata?> cache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> assemblies = new(StringComparer.Ordinal);
    private static readonly Lazy<Dictionary<string, string>> PlatformAssemblies = new(() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key!, group => group.First(), StringComparer.OrdinalIgnoreCase)
    );

    public void RegisterAssembly(string typeName, string assemblyName) => assemblies[typeName] = assemblyName;

    public PrimitiveTypeCode GetUnderlyingType(string typeName) =>
        GetMetadata(typeName)?.UnderlyingType ?? PrimitiveTypeCode.Int32;

    public string? Format(string typeName, object value)
    {
        var metadata = GetMetadata(typeName);
        if (metadata is null)
        {
            return null;
        }

        var bits = GetBits(value);
        foreach (var member in metadata.Members)
        {
            if (member.Value == bits)
            {
                return typeName + "." + member.Key;
            }
        }

        if (!metadata.IsFlags || bits == 0)
        {
            return null;
        }

        var remaining = bits;
        var selected = new List<string>();
        foreach (var member in metadata.Members.OrderByDescending(member => member.Value))
        {
            if (member.Value != 0 && (remaining & member.Value) == member.Value)
            {
                selected.Add(typeName + "." + member.Key);
                remaining &= ~member.Value;
            }
        }

        return remaining == 0 ? string.Join(" | ", selected) : null;
    }

    private EnumMetadata? GetMetadata(string typeName)
    {
        if (cache.TryGetValue(typeName, out var metadata))
        {
            return metadata;
        }

        metadata = Find(reader, typeName);
        if (metadata is null)
        {
            if (!assemblies.TryGetValue(typeName, out var assemblyName))
            {
                foreach (var handle in reader.TypeReferences)
                {
                    var reference = reader.GetTypeReference(handle);
                    if (
                        names.GetTypeFromReference(reader, handle, 0) == typeName
                        && reference.ResolutionScope.Kind == HandleKind.AssemblyReference
                    )
                    {
                        assemblyName = reader.GetString(
                            reader.GetAssemblyReference((AssemblyReferenceHandle)reference.ResolutionScope).Name
                        );
                        break;
                    }
                }
            }

            if (assemblyName is not null)
            {
                metadata = FindPlatformType(
                    assemblyName,
                    typeName,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                );
            }
        }

        cache[typeName] = metadata;
        return metadata;
    }

    private EnumMetadata? FindPlatformType(string assemblyName, string typeName, HashSet<string> visited)
    {
        if (!visited.Add(assemblyName) || !PlatformAssemblies.Value.TryGetValue(assemblyName, out var path))
        {
            return null;
        }

        // Read framework metadata only: do not load the inspected assembly or execute attribute constructors.
        try
        {
            using var stream = File.OpenRead(path);
            using var peReader = new PEReader(stream);
            var platformReader = peReader.GetMetadataReader();
            var result = Find(platformReader, typeName);
            if (result is not null)
            {
                return result;
            }

            foreach (var handle in platformReader.ExportedTypes)
            {
                var exported = platformReader.GetExportedType(handle);
                var fullName = MetadataTypeNames.FormatQualifiedMetadataName(
                    platformReader.GetString(exported.Namespace) + "." + platformReader.GetString(exported.Name)
                );
                if (fullName == typeName && exported.Implementation.Kind == HandleKind.AssemblyReference)
                {
                    var target = platformReader.GetAssemblyReference((AssemblyReferenceHandle)exported.Implementation);
                    return FindPlatformType(platformReader.GetString(target.Name), typeName, visited);
                }
            }
        }
        catch (IOException)
        {
            // An unavailable dependency should not prevent rendering the rest of the API.
        }
        catch (UnauthorizedAccessException)
        {
            // Preserve the numeric cast when framework metadata cannot be read.
        }
        catch (BadImageFormatException)
        {
            // Preserve the numeric cast for an unreadable framework dependency.
        }

        return null;
    }

    private EnumMetadata? Find(MetadataReader metadataReader, string typeName)
    {
        foreach (var handle in metadataReader.TypeDefinitions)
        {
            if (names.GetTypeFromDefinition(metadataReader, handle, 0) != typeName)
            {
                continue;
            }

            var type = metadataReader.GetTypeDefinition(handle);
            var apiMetadata = new ApiMetadataReader(metadataReader);
            if (apiMetadata.GetEntityTypeName(type.BaseType, GenericContext.Empty) != "System.Enum")
            {
                return null;
            }

            var underlying = PrimitiveTypeCode.Int32;
            var members = new List<KeyValuePair<string, ulong>>();
            foreach (var fieldHandle in type.GetFields())
            {
                var field = metadataReader.GetFieldDefinition(fieldHandle);
                if (metadataReader.GetString(field.Name) == "value__")
                {
                    underlying = field.DecodeSignature(names, GenericContext.Empty) switch
                    {
                        "byte" => PrimitiveTypeCode.Byte,
                        "sbyte" => PrimitiveTypeCode.SByte,
                        "short" => PrimitiveTypeCode.Int16,
                        "ushort" => PrimitiveTypeCode.UInt16,
                        "uint" => PrimitiveTypeCode.UInt32,
                        "long" => PrimitiveTypeCode.Int64,
                        "ulong" => PrimitiveTypeCode.UInt64,
                        _ => PrimitiveTypeCode.Int32,
                    };
                }
                else if (!field.GetDefaultValue().IsNil)
                {
                    var constant = metadataReader.GetConstant(field.GetDefaultValue());
                    var blob = metadataReader.GetBlobReader(constant.Value);
                    var bits = constant.TypeCode switch
                    {
                        ConstantTypeCode.Byte => blob.ReadByte(),
                        ConstantTypeCode.SByte => unchecked((ulong)blob.ReadSByte()),
                        ConstantTypeCode.Int16 => unchecked((ulong)blob.ReadInt16()),
                        ConstantTypeCode.UInt16 => blob.ReadUInt16(),
                        ConstantTypeCode.Int32 => unchecked((ulong)blob.ReadInt32()),
                        ConstantTypeCode.UInt32 => blob.ReadUInt32(),
                        ConstantTypeCode.Int64 => unchecked((ulong)blob.ReadInt64()),
                        ConstantTypeCode.UInt64 => blob.ReadUInt64(),
                        _ => throw new BadImageFormatException("Invalid enum constant."),
                    };
                    members.Add(
                        new(MetadataTypeNames.FormatMetadataIdentifier(metadataReader.GetString(field.Name)), bits)
                    );
                }
            }

            var isFlags = type.GetCustomAttributes()
                .Any(attribute =>
                    apiMetadata.GetAttributeTypeName(metadataReader.GetCustomAttribute(attribute).Constructor)
                    == "System.FlagsAttribute"
                );
            return new(underlying, members, isFlags);
        }

        return null;
    }

    private static ulong GetBits(object value) =>
        value is ulong unsigned ? unsigned : unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture));

    private sealed record EnumMetadata(
        PrimitiveTypeCode UnderlyingType,
        IReadOnlyList<KeyValuePair<string, ulong>> Members,
        bool IsFlags
    );
}
