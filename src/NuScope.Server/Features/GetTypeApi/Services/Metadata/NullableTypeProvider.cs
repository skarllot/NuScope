using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;

internal sealed class NullableTypeProvider : ISignatureTypeProvider<NullableType, GenericContext>
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

    public NullableType GetGenericInstantiation(NullableType genericType, ImmutableArray<NullableType> typeArguments) =>
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

    public NullableType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        new(names.GetTypeFromDefinition(reader, handle, rawTypeKind), rawTypeKind == (byte)SignatureTypeKind.ValueType);

    public NullableType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        new(names.GetTypeFromReference(reader, handle, rawTypeKind), rawTypeKind == (byte)SignatureTypeKind.ValueType);

    public NullableType GetTypeFromSpecification(
        MetadataReader reader,
        GenericContext genericContext,
        TypeSpecificationHandle handle,
        byte rawTypeKind
    ) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
}
