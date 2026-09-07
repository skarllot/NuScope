using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Metadata.MetadataTypeNames;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;

internal sealed class ApiMetadataReader(MetadataReader reader)
{
    private readonly SignatureTypeNameProvider typeNameProvider = new();
    private readonly InitOnlyTypeNameProvider initOnlyTypeNameProvider = new();

    public string? GetEntityTypeName(EntityHandle handle, GenericContext context)
    {
        if (handle.IsNil)
        {
            return null;
        }

        return handle.Kind switch
        {
            HandleKind.TypeDefinition => typeNameProvider.GetTypeFromDefinition(
                reader,
                (TypeDefinitionHandle)handle,
                0
            ),
            HandleKind.TypeReference => typeNameProvider.GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0),
            HandleKind.TypeSpecification => typeNameProvider.GetTypeFromSpecification(
                reader,
                context,
                (TypeSpecificationHandle)handle,
                0
            ),
            _ => null,
        };
    }

    public bool HasCompilerGeneratedAttribute(CustomAttributeHandleCollection handles)
    {
        foreach (var handle in handles)
        {
            var attribute = reader.GetCustomAttribute(handle);
            if (
                GetAttributeTypeName(attribute.Constructor)
                == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"
            )
            {
                return true;
            }
        }

        return false;
    }

    public string? GetAttributeTypeName(EntityHandle constructor)
    {
        EntityHandle parent = constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)constructor).Parent,
            HandleKind.MethodDefinition => reader
                .GetMethodDefinition((MethodDefinitionHandle)constructor)
                .GetDeclaringType(),
            _ => default,
        };
        return GetEntityTypeName(parent, GenericContext.Empty);
    }

    public string? GetConstant(ConstantHandle handle)
    {
        if (handle.IsNil)
        {
            return null;
        }

        var constant = reader.GetConstant(handle);
        var valueReader = reader.GetBlobReader(constant.Value);
        return constant.TypeCode switch
        {
            ConstantTypeCode.Boolean => valueReader.ReadBoolean() ? "true" : "false",
            ConstantTypeCode.Char => SymbolDisplay.FormatLiteral((char)valueReader.ReadUInt16(), quote: true),
            ConstantTypeCode.SByte => valueReader.ReadSByte().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Byte => valueReader.ReadByte().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Int16 => valueReader.ReadInt16().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt16 => valueReader.ReadUInt16().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Int32 => valueReader.ReadInt32().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt32 => valueReader.ReadUInt32().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Int64 => valueReader.ReadInt64().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt64 => valueReader.ReadUInt64().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Single => valueReader.ReadSingle().ToString("R", CultureInfo.InvariantCulture),
            ConstantTypeCode.Double => valueReader.ReadDouble().ToString("R", CultureInfo.InvariantCulture),
            ConstantTypeCode.String => SymbolDisplay.FormatLiteral(
                valueReader.ReadUTF16(valueReader.Length),
                quote: true
            ),
            ConstantTypeCode.NullReference => "null",
            _ => null,
        };
    }

    public GenericContext CreateGenericContext(
        GenericParameterHandleCollection typeParameters,
        GenericParameterHandleCollection methodParameters
    ) =>
        new(
            typeParameters
                .Select(handle => FormatMetadataIdentifier(reader.GetString(reader.GetGenericParameter(handle).Name)))
                .ToImmutableArray(),
            methodParameters
                .Select(handle => FormatMetadataIdentifier(reader.GetString(reader.GetGenericParameter(handle).Name)))
                .ToImmutableArray()
        );

    public string GetTypeKind(TypeDefinition type, string? baseType)
    {
        if ((type.Attributes & TypeAttributes.Interface) != 0)
        {
            return "interface";
        }

        return baseType switch
        {
            "System.Enum" => "enum",
            "System.MulticastDelegate" => "delegate",
            "System.ValueType" => "struct",
            _ => IsRecord(type) ? "record" : "class",
        };
    }

    private bool IsRecord(TypeDefinition type)
    {
        var hasCloneMethod = false;
        foreach (var methodHandle in type.GetMethods())
        {
            if (reader.GetString(reader.GetMethodDefinition(methodHandle).Name) == "<Clone>$")
            {
                hasCloneMethod = true;
                break;
            }
        }

        if (!hasCloneMethod)
        {
            return false;
        }

        foreach (var propertyHandle in type.GetProperties())
        {
            if (reader.GetString(reader.GetPropertyDefinition(propertyHandle).Name) == "EqualityContract")
            {
                return true;
            }
        }

        return false;
    }

    public bool IsInitOnlySetter(MethodDefinition accessor)
    {
        var signature = accessor.DecodeSignature(initOnlyTypeNameProvider, GenericContext.Empty);
        return signature.ReturnType.IsInitOnly;
    }
}
