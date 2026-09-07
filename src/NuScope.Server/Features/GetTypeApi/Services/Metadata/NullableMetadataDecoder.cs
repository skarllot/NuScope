using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;

internal sealed class NullableMetadataDecoder(MetadataReader reader, ApiMetadataReader metadata)
{
    private readonly NullableTypeProvider typeProvider = new();

    public string DecodeFieldType(FieldDefinition field, GenericContext context) =>
        FormatNullable(
            field.DecodeSignature(typeProvider, context),
            field.GetCustomAttributes(),
            GetNullableContext(field.GetDeclaringType())
        );

    public (string ReturnType, int ParameterCount) DecodeProperty(
        PropertyDefinition property,
        GenericContext context,
        TypeDefinitionHandle declaringType
    )
    {
        var signature = property.DecodeSignature(typeProvider, context);
        return (
            FormatNullable(signature.ReturnType, property.GetCustomAttributes(), GetNullableContext(declaringType)),
            signature.ParameterTypes.Length
        );
    }

    private byte[]? ReadNullableFlags(CustomAttributeHandleCollection attributes, string name)
    {
        foreach (var handle in attributes)
        {
            var attribute = reader.GetCustomAttribute(handle);
            if (metadata.GetAttributeTypeName(attribute.Constructor) != "System.Runtime.CompilerServices." + name)
            {
                continue;
            }

            var blob = reader.GetBlobReader(attribute.Value);
            if (blob.ReadUInt16() != 1)
            {
                throw new BadImageFormatException("Invalid nullable attribute prolog.");
            }

            // A scalar attribute has one byte followed by the named-argument count.
            if (blob.RemainingBytes == 3)
            {
                return [blob.ReadByte()];
            }

            var count = blob.ReadInt32();
            if (count < 0 || count > blob.RemainingBytes - 2)
            {
                throw new BadImageFormatException("Invalid nullable attribute flags.");
            }

            return blob.ReadBytes(count);
        }

        return null;
    }

    public byte GetNullableContext(TypeDefinition type) =>
        ReadNullableFlags(type.GetCustomAttributes(), "NullableContextAttribute")?[0]
        ?? GetNullableContext(type.GetDeclaringType());

    public byte GetNullableContext(EntityHandle owner)
    {
        while (!owner.IsNil)
        {
            var flags = ReadNullableFlags(reader.GetCustomAttributes(owner), "NullableContextAttribute");
            if (flags is { Length: > 0 })
            {
                return flags[0];
            }

            owner = owner.Kind switch
            {
                HandleKind.MethodDefinition => reader
                    .GetMethodDefinition((MethodDefinitionHandle)owner)
                    .GetDeclaringType(),
                HandleKind.TypeDefinition => reader.GetTypeDefinition((TypeDefinitionHandle)owner).GetDeclaringType(),
                _ => default,
            };
        }

        return 0;
    }

    public byte GetGenericParameterNullableFlag(GenericParameter parameter) =>
        ReadNullableFlags(parameter.GetCustomAttributes(), "NullableAttribute")?[0]
        ?? GetNullableContext(parameter.Parent);

    private string FormatNullable(NullableType type, CustomAttributeHandleCollection? attributes, byte context)
    {
        var flags = attributes.HasValue ? ReadNullableFlags(attributes.Value, "NullableAttribute") : null;
        var index = 0;
        return type.Render(() =>
            flags is null ? context
            : flags.Length == 1 ? flags[0]
            : index < flags.Length ? flags[index++]
            : (byte)0
        );
    }

    public (string ReturnType, ImmutableArray<string> ParameterTypes) DecodeNullableMethod(
        MethodDefinition method,
        GenericContext context
    )
    {
        var signature = method.DecodeSignature(typeProvider, context);
        var nullableContext =
            ReadNullableFlags(method.GetCustomAttributes(), "NullableContextAttribute")?[0]
            ?? GetNullableContext(method.GetDeclaringType());
        var parameters = method
            .GetParameters()
            .Select(reader.GetParameter)
            .ToDictionary(parameter => parameter.SequenceNumber);
        string Format(NullableType type, int sequence) =>
            FormatNullable(
                type,
                parameters.TryGetValue(sequence, out var parameter) ? parameter.GetCustomAttributes() : null,
                nullableContext
            );
        return (
            Format(signature.ReturnType, 0),
            signature.ParameterTypes.Select((type, index) => Format(type, index + 1)).ToImmutableArray()
        );
    }

    public string? GetNullableEntityTypeName(
        EntityHandle handle,
        GenericContext context,
        CustomAttributeHandleCollection attributes,
        byte nullableContext
    )
    {
        NullableType? type = handle.Kind switch
        {
            HandleKind.TypeDefinition => typeProvider.GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, 0),
            HandleKind.TypeReference => typeProvider.GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0),
            HandleKind.TypeSpecification => typeProvider.GetTypeFromSpecification(
                reader,
                context,
                (TypeSpecificationHandle)handle,
                0
            ),
            _ => null,
        };
        return type is null ? null : FormatNullable(type, attributes, nullableContext);
    }
}
