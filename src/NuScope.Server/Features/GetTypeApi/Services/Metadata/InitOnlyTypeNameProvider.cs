using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;

internal readonly record struct InitOnlyTypeName(bool IsInitOnly);

internal sealed class InitOnlyTypeNameProvider : ISignatureTypeProvider<InitOnlyTypeName, GenericContext>
{
    public InitOnlyTypeName GetArrayType(InitOnlyTypeName elementType, ArrayShape shape) => elementType;

    public InitOnlyTypeName GetByReferenceType(InitOnlyTypeName elementType) => elementType;

    public InitOnlyTypeName GetFunctionPointerType(MethodSignature<InitOnlyTypeName> signature) => default;

    public InitOnlyTypeName GetGenericInstantiation(
        InitOnlyTypeName genericType,
        ImmutableArray<InitOnlyTypeName> typeArguments
    ) => default;

    public InitOnlyTypeName GetGenericMethodParameter(GenericContext genericContext, int index) => default;

    public InitOnlyTypeName GetGenericTypeParameter(GenericContext genericContext, int index) => default;

    public InitOnlyTypeName GetModifiedType(
        InitOnlyTypeName modifier,
        InitOnlyTypeName unmodifiedType,
        bool isRequired
    ) => new(unmodifiedType.IsInitOnly || (isRequired && modifier.IsInitOnly));

    public InitOnlyTypeName GetPinnedType(InitOnlyTypeName elementType) => elementType;

    public InitOnlyTypeName GetPointerType(InitOnlyTypeName elementType) => elementType;

    public InitOnlyTypeName GetPrimitiveType(PrimitiveTypeCode typeCode) => default;

    public InitOnlyTypeName GetSZArrayType(InitOnlyTypeName elementType) => elementType;

    public InitOnlyTypeName GetTypeFromDefinition(
        MetadataReader reader,
        TypeDefinitionHandle handle,
        byte rawTypeKind
    ) => default;

    public InitOnlyTypeName GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var type = reader.GetTypeReference(handle);
        var isInitOnly =
            reader.GetString(type.Namespace) == "System.Runtime.CompilerServices"
            && reader.GetString(type.Name) == "IsExternalInit";
        return new(isInitOnly);
    }

    public InitOnlyTypeName GetTypeFromSpecification(
        MetadataReader reader,
        GenericContext genericContext,
        TypeSpecificationHandle handle,
        byte rawTypeKind
    ) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
}
