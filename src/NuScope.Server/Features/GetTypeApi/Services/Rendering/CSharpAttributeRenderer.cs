using System.Collections.Immutable;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Raiqub.NuScope.Features.Common.Extensions;
using Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Metadata.MetadataTypeNames;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Rendering;

internal sealed class CSharpAttributeRenderer(MetadataReader reader, ApiMetadataReader metadata)
{
    private readonly AttributeTypeProvider types = new(reader);
    private readonly HashSet<string> generatedAttributeTypes = reader
        .TypeDefinitions.Where(handle =>
            metadata.HasCompilerGeneratedAttribute(reader.GetTypeDefinition(handle).GetCustomAttributes())
        )
        .Select(handle => FormatQualifiedMetadataName(GetFullName(reader, handle).Replace('+', '.')))
        .ToHashSet(StringComparer.Ordinal);

    public bool HasAttributes(CustomAttributeHandleCollection handles) =>
        handles.Any(handle =>
            metadata.GetAttributeTypeName(reader.GetCustomAttribute(handle).Constructor) is { } name
            && !IsRepresentedOrGenerated(name)
        );

    public void AppendLines(
        StringBuilder builder,
        CustomAttributeHandleCollection handles,
        int indent,
        string? target = null
    )
    {
        foreach (var attribute in Render(handles, target))
        {
            builder.AppendIndent(indent).AppendLine(attribute);
        }
    }

    public void AppendInline(StringBuilder builder, CustomAttributeHandleCollection handles, string? target = null)
    {
        foreach (var attribute in Render(handles, target))
        {
            builder.Append(attribute).Append(' ');
        }
    }

    private IEnumerable<string> Render(CustomAttributeHandleCollection handles, string? target)
    {
        foreach (var handle in handles)
        {
            var attribute = reader.GetCustomAttribute(handle);
            var name = metadata.GetAttributeTypeName(attribute.Constructor);
            if (name is null || IsRepresentedOrGenerated(name))
            {
                continue;
            }

            var value = attribute.DecodeValue(types);
            var arguments = value
                .FixedArguments.Select(argument => FormatValue(argument.Type, argument.Value))
                .Concat(
                    value.NamedArguments.Select(argument =>
                        FormatMetadataIdentifier(argument.Name!) + " = " + FormatValue(argument.Type, argument.Value)
                    )
                );
            var argumentsText = string.Join(", ", arguments);
            yield return "["
                + (target is null ? "" : target + ": ")
                + name
                + (argumentsText.Length == 0 ? "" : "(" + argumentsText + ")")
                + "]";
        }
    }

    private bool IsRepresentedOrGenerated(string name) =>
        generatedAttributeTypes.Contains(name)
        || name
            is "System.Runtime.CompilerServices.CompilerGeneratedAttribute"
                or "System.Runtime.CompilerServices.AsyncStateMachineAttribute"
                or "System.Runtime.CompilerServices.IteratorStateMachineAttribute"
                or "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute"
                or "System.Runtime.CompilerServices.NullableAttribute"
                or "System.Runtime.CompilerServices.NullableContextAttribute"
                or "System.Runtime.CompilerServices.ExtensionAttribute"
                or "System.Runtime.CompilerServices.NativeIntegerAttribute"
                or "System.Runtime.CompilerServices.RefSafetyRulesAttribute"
                or "System.Runtime.CompilerServices.IsReadOnlyAttribute"
                or "System.Runtime.CompilerServices.IsByRefLikeAttribute"
                or "System.Runtime.CompilerServices.IsUnmanagedAttribute"
                or "System.Runtime.CompilerServices.RequiresLocationAttribute"
                or "System.ParamArrayAttribute"
                or "System.Runtime.CompilerServices.ParamCollectionAttribute"
                or "System.Reflection.DefaultMemberAttribute";

    private string FormatValue(string type, object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is ImmutableArray<CustomAttributeTypedArgument<string>> elements)
        {
            return "new "
                + type
                + " { "
                + string.Join(", ", elements.Select(element => FormatValue(element.Type, element.Value)))
                + " }";
        }

        if (type == "System.Type")
        {
            return "typeof(" + value + ")";
        }

        if (types.FormatEnum(type, value) is { } enumExpression)
        {
            return enumExpression;
        }

        var literal = value switch
        {
            string text => SymbolDisplay.FormatLiteral(text, true),
            char character => SymbolDisplay.FormatLiteral(character, true),
            bool boolean => boolean ? "true" : "false",
            float number when float.IsNaN(number) => "float.NaN",
            float number when float.IsPositiveInfinity(number) => "float.PositiveInfinity",
            float number when float.IsNegativeInfinity(number) => "float.NegativeInfinity",
            float number => number.ToString("R", CultureInfo.InvariantCulture) + "F",
            double number when double.IsNaN(number) => "double.NaN",
            double number when double.IsPositiveInfinity(number) => "double.PositiveInfinity",
            double number when double.IsNegativeInfinity(number) => "double.NegativeInfinity",
            double number => number.ToString("R", CultureInfo.InvariantCulture) + "D",
            uint number => number.ToString(CultureInfo.InvariantCulture) + "U",
            long number => number.ToString(CultureInfo.InvariantCulture) + "L",
            ulong number => number.ToString(CultureInfo.InvariantCulture) + "UL",
            byte number => (type is "byte" or "object" ? "(byte)" : "") + number.ToString(CultureInfo.InvariantCulture),
            sbyte number => (type is "sbyte" or "object" ? "(sbyte)" : "")
                + number.ToString(CultureInfo.InvariantCulture),
            short number => (type is "short" or "object" ? "(short)" : "")
                + number.ToString(CultureInfo.InvariantCulture),
            ushort number => (type is "ushort" or "object" ? "(ushort)" : "")
                + number.ToString(CultureInfo.InvariantCulture),
            IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            _ => throw new BadImageFormatException("Unsupported custom attribute argument."),
        };
        return
            type
                is "bool"
                    or "char"
                    or "string"
                    or "object"
                    or "byte"
                    or "sbyte"
                    or "short"
                    or "ushort"
                    or "int"
                    or "uint"
                    or "long"
                    or "ulong"
                    or "float"
                    or "double"
            ? literal
            : "(" + type + ")" + literal;
    }

    private sealed class AttributeTypeProvider(MetadataReader reader) : ICustomAttributeTypeProvider<string>
    {
        private readonly SignatureTypeNameProvider names = new();
        private readonly AttributeEnumMetadataResolver enums = new(reader);

        public string? FormatEnum(string type, object value) => enums.Format(type, value);

        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => names.GetPrimitiveType(typeCode);

        public string GetSystemType() => "System.Type";

        public bool IsSystemType(string type) => type == "System.Type";

        public string GetSZArrayType(string elementType) => elementType + "[]";

        public string GetTypeFromDefinition(
            MetadataReader metadataReader,
            TypeDefinitionHandle handle,
            byte rawTypeKind
        ) => names.GetTypeFromDefinition(metadataReader, handle, rawTypeKind);

        public string GetTypeFromReference(
            MetadataReader metadataReader,
            TypeReferenceHandle handle,
            byte rawTypeKind
        ) => names.GetTypeFromReference(metadataReader, handle, rawTypeKind);

        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => enums.GetUnderlyingType(type);

        public string GetTypeFromSerializedName(string name)
        {
            var parts = Split(name).ToArray();
            var typeName = Split(name).First().Trim();
            if (parts.Length > 1)
            {
                enums.RegisterAssembly(FormatQualifiedMetadataName(typeName.Replace('+', '.')), parts[1].Trim());
            }
            var bracket = typeName.IndexOf('[');
            if (bracket < 0)
            {
                var arity = typeName.LastIndexOf('`');
                return arity >= 0 && int.TryParse(typeName[(arity + 1)..], out var count)
                    ? FormatQualifiedMetadataName(typeName[..arity].Replace('+', '.'))
                        + "<"
                        + new string(',', count - 1)
                        + ">"
                    : FormatQualifiedMetadataName(typeName.Replace('+', '.'));
            }

            var suffix = typeName[bracket..];
            if (suffix.All(character => character is '[' or ']' or ',' or '*'))
            {
                return GetTypeFromSerializedName(typeName[..bracket]) + suffix;
            }

            var depth = 0;
            var end = bracket;
            for (; end < typeName.Length; end++)
            {
                if (typeName[end] == '[')
                    depth++;
                if (typeName[end] == ']' && --depth == 0)
                    break;
            }

            var arguments = Split(typeName[(bracket + 1)..end])
                .Select(argument =>
                {
                    argument = argument.Trim();
                    return GetTypeFromSerializedName(argument.StartsWith('[') ? argument[1..^1] : argument);
                })
                .ToImmutableArray();
            return names.GetGenericInstantiation(typeName[..bracket].Replace('+', '.'), arguments)
                + typeName[(end + 1)..];
        }

        private static IEnumerable<string> Split(string text)
        {
            var depth = 0;
            var start = 0;
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '[')
                    depth++;
                else if (text[index] == ']')
                    depth--;
                else if (text[index] == ',' && depth == 0)
                {
                    yield return text[start..index];
                    start = index + 1;
                }
            }

            yield return text[start..];
        }
    }
}
