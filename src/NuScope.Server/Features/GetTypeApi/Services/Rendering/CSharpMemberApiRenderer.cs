using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Raiqub.NuScope.Features.Common.Extensions;
using Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Metadata.MetadataTypeNames;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Rendering.CSharpApiModifiers;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Rendering;

internal sealed class CSharpMemberApiRenderer(
    MetadataReader reader,
    ApiMetadataReader metadata,
    NullableMetadataDecoder nullability,
    CSharpGenericDeclarations generics,
    bool includePrivate
)
{
    public void RenderFields(StringBuilder builder, TypeDefinition type, GenericContext context, int indent)
    {
        foreach (var handle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(handle);
            if (
                (field.Attributes & FieldAttributes.SpecialName) != 0
                || metadata.HasCompilerGeneratedAttribute(field.GetCustomAttributes())
                || !ShouldInclude(field.Attributes)
            )
            {
                continue;
            }

            builder.AppendIndent(indent);
            builder.Append(GetFieldVisibility(field.Attributes));
            if ((field.Attributes & FieldAttributes.Literal) != 0)
            {
                builder.Append("const ");
            }
            else
            {
                if ((field.Attributes & FieldAttributes.Static) != 0)
                {
                    builder.Append("static ");
                }

                if ((field.Attributes & FieldAttributes.InitOnly) != 0)
                {
                    builder.Append("readonly ");
                }
            }

            builder
                .Append(nullability.DecodeFieldType(field, context))
                .Append(' ')
                .Append(FormatMetadataIdentifier(reader.GetString(field.Name)));
            var constant = metadata.GetConstant(field.GetDefaultValue());
            if (constant is not null)
            {
                builder.Append(" = ").Append(constant);
            }

            builder.AppendLine(";");
        }
    }

    public void RenderConstructorsAndMethods(StringBuilder builder, TypeDefinition type, bool isInterface, int indent)
    {
        foreach (var handle in type.GetMethods())
        {
            var method = reader.GetMethodDefinition(handle);
            var name = reader.GetString(method.Name);
            var isConstructor = name is ".ctor" or ".cctor";
            var isOperator = name.StartsWith("op_", StringComparison.Ordinal);
            if (
                ((method.Attributes & MethodAttributes.SpecialName) != 0 && !isConstructor && !isOperator)
                || metadata.HasCompilerGeneratedAttribute(method.GetCustomAttributes())
                || (name != ".cctor" && !ShouldInclude(method.Attributes))
            )
            {
                continue;
            }

            var methodContext = metadata.CreateGenericContext(
                type.GetGenericParameters(),
                method.GetGenericParameters()
            );
            var signature = nullability.DecodeNullableMethod(method, methodContext);
            builder.AppendIndent(indent);
            if (!isInterface && name != ".cctor")
            {
                builder.Append(GetMethodVisibility(method.Attributes));
            }

            AppendMethodModifiers(builder, method.Attributes, isInterface);
            if (isConstructor)
            {
                builder.Append(FormatMetadataIdentifier(RemoveGenericArity(reader.GetString(type.Name))));
            }
            else if (name is "op_Implicit" or "op_Explicit" or "op_CheckedExplicit")
            {
                builder.Append(GetMethodName(name)).Append(' ').Append(signature.ReturnType);
            }
            else
            {
                builder.Append(signature.ReturnType).Append(' ').Append(GetMethodName(name));
                generics.AppendGenericParameterList(builder, method.GetGenericParameters());
            }

            AppendParameters(builder, method, signature.ParameterTypes);
            generics.AppendGenericConstraints(builder, method.GetGenericParameters(), methodContext);
            builder.AppendLine(";");
        }
    }

    public void RenderProperties(
        StringBuilder builder,
        TypeDefinition type,
        GenericContext context,
        bool isInterface,
        int indent
    )
    {
        foreach (var handle in type.GetProperties())
        {
            var property = reader.GetPropertyDefinition(handle);
            var accessors = property.GetAccessors();
            var getter = GetIncludedMethod(accessors.Getter);
            var setter = GetIncludedMethod(accessors.Setter);
            if (getter is null && setter is null)
            {
                continue;
            }

            var representative = GetMostVisible(getter, setter);
            var signature = nullability.DecodeProperty(property, context, representative!.Value.GetDeclaringType());
            builder.AppendIndent(indent);
            if (!isInterface)
            {
                builder.Append(GetMethodVisibility(representative!.Value.Attributes));
                AppendMethodModifiers(builder, representative.Value.Attributes, false);
            }

            builder.Append(signature.ReturnType).Append(' ');
            if (signature.ParameterCount == 0)
            {
                builder.Append(FormatMetadataIdentifier(reader.GetString(property.Name)));
            }
            else
            {
                builder.Append("this[");
                AppendParameterTypes(
                    builder,
                    nullability
                        .DecodeNullableMethod(representative!.Value, context)
                        .ParameterTypes.Take(signature.ParameterCount)
                        .ToImmutableArray()
                );
                builder.Append(']');
            }

            builder.Append(" { ");
            if (getter is not null)
            {
                AppendAccessor(builder, "get", getter.Value, representative!.Value, isInterface);
            }

            if (setter is not null)
            {
                AppendAccessor(
                    builder,
                    metadata.IsInitOnlySetter(setter.Value) ? "init" : "set",
                    setter.Value,
                    representative!.Value,
                    isInterface
                );
            }

            builder.AppendLine("}");
        }
    }

    public void RenderEvents(
        StringBuilder builder,
        TypeDefinition type,
        GenericContext context,
        bool isInterface,
        int indent
    )
    {
        foreach (var handle in type.GetEvents())
        {
            var @event = reader.GetEventDefinition(handle);
            var accessors = @event.GetAccessors();
            var adder = GetIncludedMethod(accessors.Adder);
            var remover = GetIncludedMethod(accessors.Remover);
            var representative = GetMostVisible(adder, remover);
            if (representative is null)
            {
                continue;
            }

            builder.AppendIndent(indent);
            if (!isInterface)
            {
                builder.Append(GetMethodVisibility(representative.Value.Attributes));
                AppendMethodModifiers(builder, representative.Value.Attributes, false);
            }

            builder
                .Append("event ")
                .Append(
                    nullability.GetNullableEntityTypeName(
                        @event.Type,
                        context,
                        @event.GetCustomAttributes(),
                        nullability.GetNullableContext(representative.Value.GetDeclaringType())
                    )
                )
                .Append(' ')
                .Append(FormatMetadataIdentifier(reader.GetString(@event.Name)))
                .AppendLine(";");
        }
    }

    public void RenderEnumMembers(StringBuilder builder, TypeDefinition type, int indent)
    {
        foreach (var handle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(handle);
            if ((field.Attributes & FieldAttributes.Literal) == 0)
            {
                continue;
            }

            builder.AppendIndent(indent);
            builder.Append(FormatMetadataIdentifier(reader.GetString(field.Name)));
            var constant = metadata.GetConstant(field.GetDefaultValue());
            if (constant is not null)
            {
                builder.Append(" = ").Append(constant);
            }

            builder.AppendLine(",");
        }
    }

    public void RenderDelegateDeclaration(
        StringBuilder builder,
        TypeDefinition type,
        GenericContext context,
        int indent
    )
    {
        var invoke = type.GetMethods()
            .Select(handle => reader.GetMethodDefinition(handle))
            .FirstOrDefault(method => reader.GetString(method.Name) == "Invoke");
        if (invoke.Name.IsNil)
        {
            return;
        }

        var signature = nullability.DecodeNullableMethod(invoke, context);
        builder.AppendIndent(indent);
        builder
            .Append(GetTypeVisibility(type.Attributes))
            .Append("delegate ")
            .Append(signature.ReturnType)
            .Append(' ')
            .Append(generics.GetTypeDeclarationName(type));
        AppendParameters(builder, invoke, signature.ParameterTypes);
        generics.AppendGenericConstraints(builder, type.GetGenericParameters(), context);
        builder.AppendLine(";");
    }

    private void AppendParameters(StringBuilder builder, MethodDefinition method, ImmutableArray<string> parameterTypes)
    {
        var parameterNames = method
            .GetParameters()
            .Select(handle => reader.GetParameter(handle))
            .Where(parameter => parameter.SequenceNumber > 0)
            .ToDictionary(parameter => parameter.SequenceNumber - 1);
        builder.Append('(');
        for (var index = 0; index < parameterTypes.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            var parameterType = parameterTypes[index];
            parameterNames.TryGetValue(index, out var parameter);
            if (
                (parameter.Attributes & ParameterAttributes.Out) != 0
                && parameterType.StartsWith("ref ", StringComparison.Ordinal)
            )
            {
                parameterType = string.Create(CultureInfo.InvariantCulture, $"out {parameterType[4..]}");
            }
            else if (
                (parameter.Attributes & ParameterAttributes.In) != 0
                && parameterType.StartsWith("ref ", StringComparison.Ordinal)
            )
            {
                parameterType = string.Create(CultureInfo.InvariantCulture, $"in {parameterType[4..]}");
            }

            builder.Append(parameterType).Append(' ');
            builder.Append(
                parameter.Name.IsNil
                    ? string.Create(CultureInfo.InvariantCulture, $"arg{index}")
                    : FormatMetadataIdentifier(reader.GetString(parameter.Name))
            );
            var defaultValue = metadata.GetConstant(parameter.GetDefaultValue());
            if (defaultValue is not null)
            {
                builder.Append(" = ").Append(defaultValue);
            }
        }

        builder.Append(')');
    }

    private static void AppendParameterTypes(StringBuilder builder, ImmutableArray<string> parameterTypes)
    {
        for (var index = 0; index < parameterTypes.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(parameterTypes[index]).Append(' ').Append("arg").Append(index);
        }
    }

    private MethodDefinition? GetIncludedMethod(MethodDefinitionHandle handle)
    {
        if (handle.IsNil)
        {
            return null;
        }

        var method = reader.GetMethodDefinition(handle);
        return ShouldInclude(method.Attributes) ? method : null;
    }

    private bool ShouldInclude(MethodAttributes attributes) =>
        includePrivate
        || (attributes & MethodAttributes.MemberAccessMask)
            is MethodAttributes.Public
                or MethodAttributes.Family
                or MethodAttributes.FamORAssem;

    private bool ShouldInclude(FieldAttributes attributes) =>
        includePrivate
        || (attributes & FieldAttributes.FieldAccessMask)
            is FieldAttributes.Public
                or FieldAttributes.Family
                or FieldAttributes.FamORAssem;
}
