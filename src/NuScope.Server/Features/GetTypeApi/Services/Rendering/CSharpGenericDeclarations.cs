using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Metadata.MetadataTypeNames;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Rendering.CSharpApiModifiers;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Rendering;

internal sealed class CSharpGenericDeclarations(
    MetadataReader reader,
    NullableMetadataDecoder nullability,
    CSharpAttributeRenderer attributes
)
{
    public void AppendGenericConstraints(
        StringBuilder builder,
        GenericParameterHandleCollection handles,
        GenericContext context
    )
    {
        foreach (var handle in handles)
        {
            var parameter = reader.GetGenericParameter(handle);
            var constraints = new List<string>();
            var hasValueTypeConstraint =
                (parameter.Attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            if ((parameter.Attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                constraints.Add(nullability.GetGenericParameterNullableFlag(parameter) == 2 ? "class?" : "class");
            }

            if (
                !hasValueTypeConstraint
                && (parameter.Attributes & GenericParameterAttributes.ReferenceTypeConstraint) == 0
                && parameter.GetConstraints().Count == 0
                && nullability.GetGenericParameterNullableFlag(parameter) == 1
            )
            {
                constraints.Add("notnull");
            }

            if (hasValueTypeConstraint)
            {
                constraints.Add("struct");
            }

            constraints.AddRange(
                parameter
                    .GetConstraints()
                    .Select(constraintHandle => reader.GetGenericParameterConstraint(constraintHandle))
                    .Select(constraint =>
                        nullability.GetNullableEntityTypeName(
                            constraint.Type,
                            context,
                            constraint.GetCustomAttributes(),
                            nullability.GetNullableContext(parameter.Parent)
                        )
                    )
                    .OfType<string>()
                    .Where(constraint => constraint != "System.ValueType")
            );
            if (
                !hasValueTypeConstraint
                && (parameter.Attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0
            )
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                builder
                    .Append(" where ")
                    .Append(FormatMetadataIdentifier(reader.GetString(parameter.Name)))
                    .Append(" : ")
                    .AppendJoin(", ", constraints);
            }
        }
    }

    public void AppendGenericParameterList(StringBuilder builder, GenericParameterHandleCollection handles)
    {
        if (handles.Count == 0)
        {
            return;
        }

        builder.Append('<');
        var first = true;
        foreach (var handle in handles)
        {
            if (!first)
                builder.Append(", ");
            first = false;
            var parameter = reader.GetGenericParameter(handle);
            attributes.AppendInline(builder, parameter.GetCustomAttributes());
            builder.Append(FormatMetadataIdentifier(reader.GetString(parameter.Name)));
        }
        builder.Append('>');
    }

    public string GetTypeDeclarationName(TypeDefinition type)
    {
        var builder = new StringBuilder(FormatMetadataIdentifier(RemoveGenericArity(reader.GetString(type.Name))));
        AppendGenericParameterList(builder, type.GetGenericParameters());
        return builder.ToString();
    }
}
