using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Raiqub.NuScope.Features.Common.Extensions;

namespace Raiqub.NuScope.Features.GetTypeApi.Services;

public sealed class NuGetTypeApiReader : INuGetTypeApiReader
{
    public string? ReadTypeApi(Stream stream, string fullTypeName, bool includePrivate)
    {
        var (bufferedStream, ownsBufferedStream) = GetSeekableStream(stream);
        try
        {
            using var peReader = new PEReader(bufferedStream, PEStreamOptions.LeaveOpen);
            if (!peReader.HasMetadata)
            {
                throw new BadImageFormatException("Assembly does not contain metadata.");
            }

            var reader = peReader.GetMetadataReader();
            foreach (var handle in reader.TypeDefinitions)
            {
                var type = reader.GetTypeDefinition(handle);
                if (
                    string.Equals(GetFullName(reader, handle), fullTypeName, StringComparison.Ordinal)
                    && (includePrivate || IsPublicApi(type.Attributes))
                )
                {
                    return new ApiRenderer(reader, includePrivate).Render(handle);
                }
            }

            return null;
        }
        finally
        {
            if (ownsBufferedStream)
            {
                bufferedStream.Dispose();
            }
        }
    }

    private static (Stream stream, bool ownsStream) GetSeekableStream(Stream stream)
    {
        if (stream.CanSeek)
        {
            return (stream, false);
        }

        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        return (memoryStream, true);
    }

    private static bool IsPublicApi(TypeAttributes attributes)
    {
        var visibility = attributes & TypeAttributes.VisibilityMask;
        return visibility
            is TypeAttributes.Public
                or TypeAttributes.NestedPublic
                or TypeAttributes.NestedFamily
                or TypeAttributes.NestedFamORAssem;
    }

    private static string GetFullName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        var name = reader.GetString(type.Name);
        if (type.IsNested)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{GetFullName(reader, type.GetDeclaringType())}+{name}"
            );
        }

        var @namespace = reader.GetString(type.Namespace);
        return string.IsNullOrEmpty(@namespace)
            ? name
            : string.Create(CultureInfo.InvariantCulture, $"{@namespace}.{name}");
    }

    private static string FormatMetadataIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return identifier;
        }

        var arityIndex = identifier.IndexOf('`');
        if (arityIndex >= 0)
        {
            var unqualifiedName = identifier[..arityIndex];
            var aritySuffix = identifier[arityIndex..];
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{FormatMetadataIdentifier(unqualifiedName)}{aritySuffix}"
            );
        }

        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
                || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? string.Create(CultureInfo.InvariantCulture, $"@{identifier}")
            : identifier;
    }

    private static string FormatQualifiedMetadataName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var segments = name.Split('.');
        for (var index = 0; index < segments.Length; index++)
        {
            segments[index] = FormatMetadataIdentifier(segments[index]);
        }

        return string.Join(".", segments);
    }

    private sealed class ApiRenderer(MetadataReader reader, bool includePrivate)
    {
        private readonly SignatureTypeNameProvider typeNameProvider = new();

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
            var context = CreateGenericContext(type.GetGenericParameters(), default);
            var baseType = GetEntityTypeName(type.BaseType, context);
            var kind = GetTypeKind(type, baseType);

            if (kind == "delegate")
            {
                RenderDelegateDeclaration(builder, type, context, indent);
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

            builder.Append(kind).Append(' ').Append(GetTypeDeclarationName(type));
            AppendBaseTypes(builder, type, kind, baseType, context);
            AppendGenericConstraints(builder, type.GetGenericParameters(), context);
            builder.AppendLine().AppendIndent(indent).AppendLine("{");

            if (kind == "enum")
            {
                RenderEnumMembers(builder, type, indent + 1);
            }
            else
            {
                RenderFields(builder, type, context, indent + 1);
                RenderConstructorsAndMethods(builder, type, kind == "interface", indent + 1);
                RenderProperties(builder, type, context, kind == "interface", indent + 1);
                RenderEvents(builder, type, context, kind == "interface", indent + 1);
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
                baseTypes.Add(baseType);
            }

            if (kind is not "enum" and not "delegate")
            {
                baseTypes.AddRange(
                    type.GetInterfaceImplementations()
                        .Select(handle => reader.GetInterfaceImplementation(handle))
                        .Select(implementation => GetEntityTypeName(implementation.Interface, context))
                        .OfType<string>()
                );
            }

            if (baseTypes.Count > 0)
            {
                builder.Append(" : ").AppendJoin(", ", baseTypes);
            }
        }

        private void RenderFields(StringBuilder builder, TypeDefinition type, GenericContext context, int indent)
        {
            foreach (var handle in type.GetFields())
            {
                var field = reader.GetFieldDefinition(handle);
                if (
                    (field.Attributes & FieldAttributes.SpecialName) != 0
                    || HasCompilerGeneratedAttribute(field.GetCustomAttributes())
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
                    .Append(field.DecodeSignature(typeNameProvider, context))
                    .Append(' ')
                    .Append(FormatMetadataIdentifier(reader.GetString(field.Name)));
                var constant = GetConstant(field.GetDefaultValue());
                if (constant is not null)
                {
                    builder.Append(" = ").Append(constant);
                }

                builder.AppendLine(";");
            }
        }

        private void RenderConstructorsAndMethods(
            StringBuilder builder,
            TypeDefinition type,
            bool isInterface,
            int indent
        )
        {
            foreach (var handle in type.GetMethods())
            {
                var method = reader.GetMethodDefinition(handle);
                var name = reader.GetString(method.Name);
                var isConstructor = name is ".ctor" or ".cctor";
                var isOperator = name.StartsWith("op_", StringComparison.Ordinal);
                if (
                    ((method.Attributes & MethodAttributes.SpecialName) != 0 && !isConstructor && !isOperator)
                    || HasCompilerGeneratedAttribute(method.GetCustomAttributes())
                    || (name != ".cctor" && !ShouldInclude(method.Attributes))
                )
                {
                    continue;
                }

                var methodContext = CreateGenericContext(type.GetGenericParameters(), method.GetGenericParameters());
                var signature = method.DecodeSignature(typeNameProvider, methodContext);
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
                else if (name is "op_Implicit" or "op_Explicit")
                {
                    builder.Append(GetMethodName(name)).Append(' ').Append(signature.ReturnType);
                }
                else
                {
                    builder.Append(signature.ReturnType).Append(' ').Append(GetMethodName(name));
                    AppendGenericParameterList(builder, method.GetGenericParameters());
                }

                AppendParameters(builder, method, signature.ParameterTypes);
                AppendGenericConstraints(builder, method.GetGenericParameters(), methodContext);
                builder.AppendLine(";");
            }
        }

        private void RenderProperties(
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

                var signature = property.DecodeSignature(typeNameProvider, context);
                var representative = GetMostVisible(getter, setter);
                builder.AppendIndent(indent);
                if (!isInterface)
                {
                    builder.Append(GetMethodVisibility(representative!.Value.Attributes));
                    AppendMethodModifiers(builder, representative.Value.Attributes, false);
                }

                builder.Append(signature.ReturnType).Append(' ');
                if (signature.ParameterTypes.Length == 0)
                {
                    builder.Append(FormatMetadataIdentifier(reader.GetString(property.Name)));
                }
                else
                {
                    builder.Append("this[");
                    AppendParameterTypes(builder, signature.ParameterTypes);
                    builder.Append(']');
                }

                builder.Append(" { ");
                if (getter is not null)
                {
                    AppendAccessor(builder, "get", getter.Value, representative!.Value, isInterface);
                }

                if (setter is not null)
                {
                    AppendAccessor(builder, "set", setter.Value, representative!.Value, isInterface);
                }

                builder.AppendLine("}");
            }
        }

        private void RenderEvents(
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
                    .Append(GetEntityTypeName(@event.Type, context))
                    .Append(' ')
                    .Append(FormatMetadataIdentifier(reader.GetString(@event.Name)))
                    .AppendLine(";");
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
                    && !HasCompilerGeneratedAttribute(nested.GetCustomAttributes())
                )
                {
                    RenderType(builder, handle, indent);
                }
            }
        }

        private void RenderEnumMembers(StringBuilder builder, TypeDefinition type, int indent)
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
                var constant = GetConstant(field.GetDefaultValue());
                if (constant is not null)
                {
                    builder.Append(" = ").Append(constant);
                }

                builder.AppendLine(",");
            }
        }

        private void RenderDelegateDeclaration(
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

            var signature = invoke.DecodeSignature(typeNameProvider, context);
            builder.AppendIndent(indent);
            builder
                .Append(GetTypeVisibility(type.Attributes))
                .Append("delegate ")
                .Append(signature.ReturnType)
                .Append(' ')
                .Append(GetTypeDeclarationName(type));
            AppendParameters(builder, invoke, signature.ParameterTypes);
            AppendGenericConstraints(builder, type.GetGenericParameters(), context);
            builder.AppendLine(";");
        }

        private void AppendParameters(
            StringBuilder builder,
            MethodDefinition method,
            ImmutableArray<string> parameterTypes
        )
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
                var defaultValue = GetConstant(parameter.GetDefaultValue());
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

        private void AppendGenericConstraints(
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
                    constraints.Add("class");
                }

                if (hasValueTypeConstraint)
                {
                    constraints.Add("struct");
                }

                constraints.AddRange(
                    parameter
                        .GetConstraints()
                        .Select(constraintHandle => reader.GetGenericParameterConstraint(constraintHandle))
                        .Select(constraint => GetEntityTypeName(constraint.Type, context))
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

        private void AppendGenericParameterList(StringBuilder builder, GenericParameterHandleCollection handles)
        {
            var names = handles
                .Select(handle => FormatMetadataIdentifier(reader.GetString(reader.GetGenericParameter(handle).Name)))
                .ToArray();
            if (names.Length > 0)
            {
                builder.Append('<').AppendJoin(", ", names).Append('>');
            }
        }

        private string GetTypeDeclarationName(TypeDefinition type)
        {
            var builder = new StringBuilder(
                FormatMetadataIdentifier(RemoveGenericArity(reader.GetString(type.Name)))
            );
            AppendGenericParameterList(builder, type.GetGenericParameters());
            return builder.ToString();
        }

        private string? GetEntityTypeName(EntityHandle handle, GenericContext context)
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
                HandleKind.TypeReference => typeNameProvider.GetTypeFromReference(
                    reader,
                    (TypeReferenceHandle)handle,
                    0
                ),
                HandleKind.TypeSpecification => typeNameProvider.GetTypeFromSpecification(
                    reader,
                    context,
                    (TypeSpecificationHandle)handle,
                    0
                ),
                _ => null,
            };
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

        private static MethodDefinition? GetMostVisible(MethodDefinition? left, MethodDefinition? right)
        {
            if (left is null)
            {
                return right;
            }

            if (right is null)
            {
                return left;
            }

            return GetVisibilityRank(left.Value.Attributes) >= GetVisibilityRank(right.Value.Attributes) ? left : right;
        }

        private static void AppendAccessor(
            StringBuilder builder,
            string keyword,
            MethodDefinition accessor,
            MethodDefinition representative,
            bool isInterface
        )
        {
            if (!isInterface && GetVisibilityRank(accessor.Attributes) < GetVisibilityRank(representative.Attributes))
            {
                builder.Append(GetMethodVisibility(accessor.Attributes));
            }

            builder.Append(keyword).Append("; ");
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

        private bool HasCompilerGeneratedAttribute(CustomAttributeHandleCollection handles)
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

        private string? GetAttributeTypeName(EntityHandle constructor)
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

        private string? GetConstant(ConstantHandle handle)
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

        private GenericContext CreateGenericContext(
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

        private string GetTypeKind(TypeDefinition type, string? baseType)
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

        private static string GetTypeVisibility(TypeAttributes attributes) =>
            (attributes & TypeAttributes.VisibilityMask) switch
            {
                TypeAttributes.Public or TypeAttributes.NestedPublic => "public ",
                TypeAttributes.NestedPrivate => "private ",
                TypeAttributes.NestedFamily => "protected ",
                TypeAttributes.NestedFamORAssem => "protected internal ",
                TypeAttributes.NestedFamANDAssem => "private protected ",
                _ => "internal ",
            };

        private static string GetMethodVisibility(MethodAttributes attributes) =>
            (attributes & MethodAttributes.MemberAccessMask) switch
            {
                MethodAttributes.Public => "public ",
                MethodAttributes.Private => "private ",
                MethodAttributes.Family => "protected ",
                MethodAttributes.FamORAssem => "protected internal ",
                MethodAttributes.FamANDAssem => "private protected ",
                _ => "internal ",
            };

        private static string GetFieldVisibility(FieldAttributes attributes) =>
            (attributes & FieldAttributes.FieldAccessMask) switch
            {
                FieldAttributes.Public => "public ",
                FieldAttributes.Private => "private ",
                FieldAttributes.Family => "protected ",
                FieldAttributes.FamORAssem => "protected internal ",
                FieldAttributes.FamANDAssem => "private protected ",
                _ => "internal ",
            };

        private static int GetVisibilityRank(MethodAttributes attributes) =>
            (attributes & MethodAttributes.MemberAccessMask) switch
            {
                MethodAttributes.Public => 6,
                MethodAttributes.FamORAssem => 5,
                MethodAttributes.Family => 4,
                MethodAttributes.Assembly => 3,
                MethodAttributes.FamANDAssem => 2,
                _ => 1,
            };

        private static void AppendMethodModifiers(StringBuilder builder, MethodAttributes attributes, bool isInterface)
        {
            if ((attributes & MethodAttributes.Static) != 0)
            {
                builder.Append("static ");
            }

            if (isInterface)
            {
                return;
            }

            var isAbstract = (attributes & MethodAttributes.Abstract) != 0;
            var isVirtual = (attributes & MethodAttributes.Virtual) != 0;
            var isOverride = isVirtual && (attributes & MethodAttributes.NewSlot) == 0;
            var isSealedOverride = isOverride && (attributes & MethodAttributes.Final) != 0;
            if (isAbstract)
            {
                builder.Append("abstract ");
            }

            if (isSealedOverride)
            {
                builder.Append("sealed ");
            }

            if (isOverride)
            {
                builder.Append("override ");
            }
            else if (isVirtual && !isAbstract && (attributes & MethodAttributes.Final) == 0)
            {
                builder.Append("virtual ");
            }
        }

        private static string GetMethodName(string metadataName) =>
            metadataName switch
            {
                "op_Addition" => "operator +",
                "op_Subtraction" => "operator -",
                "op_Multiply" => "operator *",
                "op_Division" => "operator /",
                "op_Equality" => "operator ==",
                "op_Inequality" => "operator !=",
                "op_Implicit" => "implicit operator",
                "op_Explicit" => "explicit operator",
                _ => FormatMetadataIdentifier(metadataName),
            };

        private static string RemoveGenericArity(string name)
        {
            var index = name.IndexOf('`');
            return index < 0 ? name : name[..index];
        }
    }

    private readonly record struct GenericContext(
        ImmutableArray<string> TypeParameters,
        ImmutableArray<string> MethodParameters
    )
    {
        public static GenericContext Empty { get; } = new([], []);
    }

    private sealed class SignatureTypeNameProvider : ISignatureTypeProvider<string, GenericContext>
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

        public string GetPointerType(string elementType) =>
            string.Create(CultureInfo.InvariantCulture, $"{elementType}*");

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

        public string GetSZArrayType(string elementType) =>
            string.Create(CultureInfo.InvariantCulture, $"{elementType}[]");

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
}
