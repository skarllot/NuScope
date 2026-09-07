using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Raiqub.NuScope.Features.GetTypeApi.Services;

public sealed partial class NuGetTypeApiReader
{
    private sealed partial class ApiRenderer
    {
        private readonly NullableTypeProvider nullableTypeProvider = new();

        private byte[]? ReadNullableFlags(CustomAttributeHandleCollection attributes, string name)
        {
            foreach (var handle in attributes)
            {
                var attribute = reader.GetCustomAttribute(handle);
                if (GetAttributeTypeName(attribute.Constructor) != "System.Runtime.CompilerServices." + name)
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

        private byte GetNullableContext(TypeDefinition type) =>
            ReadNullableFlags(type.GetCustomAttributes(), "NullableContextAttribute")?[0]
            ?? GetNullableContext(type.GetDeclaringType());

        private byte GetNullableContext(EntityHandle owner)
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
                    HandleKind.TypeDefinition => reader
                        .GetTypeDefinition((TypeDefinitionHandle)owner)
                        .GetDeclaringType(),
                    _ => default,
                };
            }

            return 0;
        }

        private byte GetGenericParameterNullableFlag(GenericParameter parameter) =>
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

        private (string ReturnType, ImmutableArray<string> ParameterTypes) DecodeNullableMethod(
            MethodDefinition method,
            GenericContext context
        )
        {
            var signature = method.DecodeSignature(nullableTypeProvider, context);
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

        private string? GetNullableEntityTypeName(
            EntityHandle handle,
            GenericContext context,
            CustomAttributeHandleCollection attributes,
            byte nullableContext
        )
        {
            NullableType? type = handle.Kind switch
            {
                HandleKind.TypeDefinition => nullableTypeProvider.GetTypeFromDefinition(
                    reader,
                    (TypeDefinitionHandle)handle,
                    0
                ),
                HandleKind.TypeReference => nullableTypeProvider.GetTypeFromReference(
                    reader,
                    (TypeReferenceHandle)handle,
                    0
                ),
                HandleKind.TypeSpecification => nullableTypeProvider.GetTypeFromSpecification(
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

    // Retain the signature tree until flags can be consumed in metadata's pre-order traversal.
    private sealed record NullableType(
        string Name,
        bool IsValueType = false,
        Func<Func<byte>, string>? Formatter = null
    )
    {
        public string Render(Func<byte> next) =>
            Formatter is not null ? Formatter(next) : Name + (!IsValueType && next() == 2 ? "?" : "");
    }

    private sealed class NullableTypeProvider : ISignatureTypeProvider<NullableType, GenericContext>
    {
        private readonly SignatureTypeNameProvider names = new();

        public NullableType GetArrayType(NullableType elementType, ArrayShape shape) =>
            Array(elementType, "[" + new string(',', shape.Rank - 1) + "]");

        public NullableType GetSZArrayType(NullableType elementType) => Array(elementType, "[]");

        private static NullableType Array(NullableType element, string suffix) =>
            new(
                "",
                Formatter: next =>
                {
                    var flag = next();
                    return element.Render(next) + suffix + (flag == 2 ? "?" : "");
                }
            );

        public NullableType GetByReferenceType(NullableType elementType) =>
            new("", Formatter: next => "ref " + elementType.Render(next));

        public NullableType GetPointerType(NullableType elementType) =>
            new("", Formatter: next => elementType.Render(next) + "*");

        public NullableType GetFunctionPointerType(MethodSignature<NullableType> signature) => new("delegate*", true);

        public NullableType GetGenericInstantiation(
            NullableType genericType,
            ImmutableArray<NullableType> typeArguments
        ) =>
            new(
                genericType.Name,
                genericType.IsValueType,
                next =>
                {
                    var flag = genericType.Name == "System.Nullable`1" ? (byte)0 : next();
                    var arguments = typeArguments.Select(argument => argument.Render(next)).ToImmutableArray();
                    return names.GetGenericInstantiation(genericType.Name, arguments)
                        + (!genericType.IsValueType && flag == 2 ? "?" : "");
                }
            );

        public NullableType GetGenericMethodParameter(GenericContext genericContext, int index) =>
            new(names.GetGenericMethodParameter(genericContext, index));

        public NullableType GetGenericTypeParameter(GenericContext genericContext, int index) =>
            new(names.GetGenericTypeParameter(genericContext, index));

        public NullableType GetModifiedType(NullableType modifier, NullableType unmodifiedType, bool isRequired) =>
            unmodifiedType;

        public NullableType GetPinnedType(NullableType elementType) => elementType;

        public NullableType GetPrimitiveType(PrimitiveTypeCode typeCode) =>
            new(
                names.GetPrimitiveType(typeCode),
                typeCode is not PrimitiveTypeCode.String and not PrimitiveTypeCode.Object
            );

        public NullableType GetTypeFromDefinition(
            MetadataReader reader,
            TypeDefinitionHandle handle,
            byte rawTypeKind
        ) =>
            new(
                names.GetTypeFromDefinition(reader, handle, rawTypeKind),
                rawTypeKind == (byte)SignatureTypeKind.ValueType
            );

        public NullableType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            new(
                names.GetTypeFromReference(reader, handle, rawTypeKind),
                rawTypeKind == (byte)SignatureTypeKind.ValueType
            );

        public NullableType GetTypeFromSpecification(
            MetadataReader reader,
            GenericContext genericContext,
            TypeSpecificationHandle handle,
            byte rawTypeKind
        ) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }
}
