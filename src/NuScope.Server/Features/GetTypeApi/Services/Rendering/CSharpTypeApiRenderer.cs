using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Raiqub.NuScope.Features.Common.Extensions;
using Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Metadata.MetadataTypeNames;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Rendering.CSharpApiModifiers;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Rendering;

internal sealed class CSharpTypeApiRenderer
{
    private readonly MetadataReader reader;
    private readonly bool includePrivate;
    private readonly ApiMetadataReader metadata;
    private readonly NullableMetadataDecoder nullability;
    private readonly CSharpGenericDeclarations generics;
    private readonly CSharpMemberApiRenderer members;
    private readonly SignatureTypeNameProvider typeNameProvider = new();

    public CSharpTypeApiRenderer(MetadataReader reader, bool includePrivate)
    {
        this.reader = reader;
        this.includePrivate = includePrivate;
        metadata = new ApiMetadataReader(reader);
        nullability = new NullableMetadataDecoder(reader, metadata);
        generics = new CSharpGenericDeclarations(reader, nullability);
        members = new CSharpMemberApiRenderer(reader, metadata, nullability, generics, includePrivate);
    }

    public string Render(TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        var @namespace = FormatQualifiedMetadataName(reader.GetString(type.Namespace));
        var builder = new StringBuilder();
        if (!string.IsNullOrEmpty(@namespace))
        {
            builder.Append("namespace ").AppendLine(@namespace).AppendLine("{");
            RenderType(builder, handle, 1);
            builder.AppendLine("}");
        }
        else
        {
            RenderType(builder, handle, 0);
        }

        return builder.ToString();
    }

    private void RenderType(StringBuilder builder, TypeDefinitionHandle handle, int indent)
    {
        var type = reader.GetTypeDefinition(handle);
        var context = metadata.CreateGenericContext(type.GetGenericParameters(), default);
        var baseType = metadata.GetEntityTypeName(type.BaseType, context);
        var kind = metadata.GetTypeKind(type, baseType);

        if (kind == "delegate")
        {
            members.RenderDelegateDeclaration(builder, type, context, indent);
            return;
        }

        builder.AppendIndent(indent);
        builder.Append(GetTypeVisibility(type.Attributes));
        if (kind is "class" or "record")
        {
            var isAbstract = (type.Attributes & TypeAttributes.Abstract) != 0;
            var isSealed = (type.Attributes & TypeAttributes.Sealed) != 0;
            if (kind == "class" && isAbstract && isSealed)
            {
                builder.Append("static ");
            }
            else
            {
                if (isAbstract)
                {
                    builder.Append("abstract ");
                }

                if (isSealed)
                {
                    builder.Append("sealed ");
                }
            }
        }

        builder.Append(kind).Append(' ').Append(generics.GetTypeDeclarationName(type));
        AppendBaseTypes(builder, type, kind, baseType, context);
        generics.AppendGenericConstraints(builder, type.GetGenericParameters(), context);
        builder.AppendLine().AppendIndent(indent).AppendLine("{");

        if (kind == "enum")
        {
            members.RenderEnumMembers(builder, type, indent + 1);
        }
        else
        {
            members.RenderFields(builder, type, context, indent + 1);
            members.RenderConstructorsAndMethods(builder, type, kind == "interface", indent + 1);
            members.RenderProperties(builder, type, context, kind == "interface", indent + 1);
            members.RenderEvents(builder, type, context, kind == "interface", indent + 1);
            RenderNestedTypes(builder, handle, indent + 1);
        }

        builder.AppendIndent(indent).AppendLine("}");
    }

    private void AppendBaseTypes(
        StringBuilder builder,
        TypeDefinition type,
        string kind,
        string? baseType,
        GenericContext context
    )
    {
        var baseTypes = new List<string>();
        if (kind == "enum")
        {
            var underlyingType = type.GetFields()
                .Select(handle => reader.GetFieldDefinition(handle))
                .Where(field => string.Equals(reader.GetString(field.Name), "value__", StringComparison.Ordinal))
                .Select(field => field.DecodeSignature(typeNameProvider, context))
                .FirstOrDefault();
            if (underlyingType is not null and not "int" and not "System.Int32")
            {
                baseTypes.Add(underlyingType);
            }
        }

        if (
            baseType is not null
            && baseType
                is not "System.Object"
                    and not "System.ValueType"
                    and not "System.Enum"
                    and not "System.MulticastDelegate"
        )
        {
            baseTypes.Add(
                nullability.GetNullableEntityTypeName(
                    type.BaseType,
                    context,
                    type.GetCustomAttributes(),
                    nullability.GetNullableContext(type)
                )!
            );
        }

        if (kind is not "enum" and not "delegate")
        {
            baseTypes.AddRange(
                type.GetInterfaceImplementations()
                    .Select(handle => reader.GetInterfaceImplementation(handle))
                    .Select(implementation =>
                        nullability.GetNullableEntityTypeName(
                            implementation.Interface,
                            context,
                            implementation.GetCustomAttributes(),
                            nullability.GetNullableContext(type)
                        )
                    )
                    .OfType<string>()
            );
        }

        if (baseTypes.Count > 0)
        {
            builder.Append(" : ").AppendJoin(", ", baseTypes);
        }
    }

    private void RenderNestedTypes(StringBuilder builder, TypeDefinitionHandle declaringHandle, int indent)
    {
        foreach (var handle in reader.TypeDefinitions)
        {
            var nested = reader.GetTypeDefinition(handle);
            if (
                nested.IsNested
                && nested.GetDeclaringType() == declaringHandle
                && (includePrivate || IsPublicApi(nested.Attributes))
                && !metadata.HasCompilerGeneratedAttribute(nested.GetCustomAttributes())
            )
            {
                RenderType(builder, handle, indent);
            }
        }
    }
}
