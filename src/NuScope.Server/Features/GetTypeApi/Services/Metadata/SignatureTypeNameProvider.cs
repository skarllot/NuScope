using System.Collections.Immutable;
using System.Globalization;
using System.Reflection.Metadata;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Metadata.MetadataTypeNames;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;

internal sealed class SignatureTypeNameProvider : ISignatureTypeProvider<string, GenericContext>
{
    public string GetArrayType(string elementType, ArrayShape shape) =>
        string.Create(CultureInfo.InvariantCulture, $"{elementType}[{new string(',', shape.Rank - 1)}]");

    public string GetByReferenceType(string elementType) =>
        string.Create(CultureInfo.InvariantCulture, $"ref {elementType}");

    public string GetFunctionPointerType(MethodSignature<string> signature) => "delegate*";

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{RemoveGenericArity(genericType)}<{string.Join(", ", typeArguments)}>"
        );

    public string GetGenericMethodParameter(GenericContext genericContext, int index) =>
        index < genericContext.MethodParameters.Length
            ? genericContext.MethodParameters[index]
            : string.Create(CultureInfo.InvariantCulture, $"!!{index}");

    public string GetGenericTypeParameter(GenericContext genericContext, int index) =>
        index < genericContext.TypeParameters.Length
            ? genericContext.TypeParameters[index]
            : string.Create(CultureInfo.InvariantCulture, $"!{index}");

    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

    public string GetPinnedType(string elementType) => elementType;

    public string GetPointerType(string elementType) => string.Create(CultureInfo.InvariantCulture, $"{elementType}*");

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) =>
        typeCode switch
        {
            PrimitiveTypeCode.Boolean => "bool",
            PrimitiveTypeCode.Byte => "byte",
            PrimitiveTypeCode.Char => "char",
            PrimitiveTypeCode.Double => "double",
            PrimitiveTypeCode.Int16 => "short",
            PrimitiveTypeCode.Int32 => "int",
            PrimitiveTypeCode.Int64 => "long",
            PrimitiveTypeCode.IntPtr => "nint",
            PrimitiveTypeCode.Object => "object",
            PrimitiveTypeCode.SByte => "sbyte",
            PrimitiveTypeCode.Single => "float",
            PrimitiveTypeCode.String => "string",
            PrimitiveTypeCode.TypedReference => "System.TypedReference",
            PrimitiveTypeCode.UInt16 => "ushort",
            PrimitiveTypeCode.UInt32 => "uint",
            PrimitiveTypeCode.UInt64 => "ulong",
            PrimitiveTypeCode.UIntPtr => "nuint",
            PrimitiveTypeCode.Void => "void",
            _ => typeCode.ToString(),
        };

    public string GetSZArrayType(string elementType) => string.Create(CultureInfo.InvariantCulture, $"{elementType}[]");

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        FormatQualifiedMetadataName(GetFullName(reader, handle).Replace('+', '.'));

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var type = reader.GetTypeReference(handle);
        var name = FormatMetadataIdentifier(reader.GetString(type.Name));
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{GetTypeFromReference(reader, (TypeReferenceHandle)type.ResolutionScope, rawTypeKind)}.{name}"
            );
        }

        var @namespace = FormatQualifiedMetadataName(reader.GetString(type.Namespace));
        return string.IsNullOrEmpty(@namespace)
            ? name
            : string.Create(CultureInfo.InvariantCulture, $"{@namespace}.{name}");
    }

    public string GetTypeFromSpecification(
        MetadataReader reader,
        GenericContext genericContext,
        TypeSpecificationHandle handle,
        byte rawTypeKind
    ) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    private static string RemoveGenericArity(string name)
    {
        var index = name.IndexOf('`');
        return index < 0 ? name : name[..index];
    }
}
