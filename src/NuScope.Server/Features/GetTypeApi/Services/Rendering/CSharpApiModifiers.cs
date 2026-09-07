using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Metadata.MetadataTypeNames;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Rendering;

internal static class CSharpApiModifiers
{
    public static bool IsPublicApi(TypeAttributes attributes)
    {
        var visibility = attributes & TypeAttributes.VisibilityMask;
        return visibility
            is TypeAttributes.Public
                or TypeAttributes.NestedPublic
                or TypeAttributes.NestedFamily
                or TypeAttributes.NestedFamORAssem;
    }

    public static MethodDefinition? GetMostVisible(MethodDefinition? left, MethodDefinition? right)
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

    public static void AppendAccessor(
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

    public static string GetTypeVisibility(TypeAttributes attributes) =>
        (attributes & TypeAttributes.VisibilityMask) switch
        {
            TypeAttributes.Public or TypeAttributes.NestedPublic => "public ",
            TypeAttributes.NestedPrivate => "private ",
            TypeAttributes.NestedFamily => "protected ",
            TypeAttributes.NestedFamORAssem => "protected internal ",
            TypeAttributes.NestedFamANDAssem => "private protected ",
            _ => "internal ",
        };

    public static string GetMethodVisibility(MethodAttributes attributes) =>
        (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => "public ",
            MethodAttributes.Private => "private ",
            MethodAttributes.Family => "protected ",
            MethodAttributes.FamORAssem => "protected internal ",
            MethodAttributes.FamANDAssem => "private protected ",
            _ => "internal ",
        };

    public static string GetFieldVisibility(FieldAttributes attributes) =>
        (attributes & FieldAttributes.FieldAccessMask) switch
        {
            FieldAttributes.Public => "public ",
            FieldAttributes.Private => "private ",
            FieldAttributes.Family => "protected ",
            FieldAttributes.FamORAssem => "protected internal ",
            FieldAttributes.FamANDAssem => "private protected ",
            _ => "internal ",
        };

    public static int GetVisibilityRank(MethodAttributes attributes) =>
        (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => 6,
            MethodAttributes.FamORAssem => 5,
            MethodAttributes.Family => 4,
            MethodAttributes.Assembly => 3,
            MethodAttributes.FamANDAssem => 2,
            _ => 1,
        };

    public static void AppendMethodModifiers(StringBuilder builder, MethodAttributes attributes, bool isInterface)
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

    public static string GetMethodName(string metadataName) =>
        metadataName switch
        {
            "op_UnaryPlus" => "operator +",
            "op_UnaryNegation" => "operator -",
            "op_LogicalNot" => "operator !",
            "op_OnesComplement" => "operator ~",
            "op_Increment" => "operator ++",
            "op_Decrement" => "operator --",
            "op_True" => "operator true",
            "op_False" => "operator false",
            "op_Addition" => "operator +",
            "op_Subtraction" => "operator -",
            "op_Multiply" => "operator *",
            "op_Division" => "operator /",
            "op_Modulus" => "operator %",
            "op_BitwiseAnd" => "operator &",
            "op_BitwiseOr" => "operator |",
            "op_ExclusiveOr" => "operator ^",
            "op_LeftShift" => "operator <<",
            "op_RightShift" => "operator >>",
            "op_UnsignedRightShift" => "operator >>>",
            "op_Equality" => "operator ==",
            "op_Inequality" => "operator !=",
            "op_LessThan" => "operator <",
            "op_GreaterThan" => "operator >",
            "op_LessThanOrEqual" => "operator <=",
            "op_GreaterThanOrEqual" => "operator >=",
            "op_CheckedUnaryNegation" => "operator checked -",
            "op_CheckedIncrement" => "operator checked ++",
            "op_CheckedDecrement" => "operator checked --",
            "op_CheckedAddition" => "operator checked +",
            "op_CheckedSubtraction" => "operator checked -",
            "op_CheckedMultiply" => "operator checked *",
            "op_CheckedDivision" => "operator checked /",
            "op_AdditionAssignment" => "operator +=",
            "op_SubtractionAssignment" => "operator -=",
            "op_MultiplicationAssignment" => "operator *=",
            "op_DivisionAssignment" => "operator /=",
            "op_ModulusAssignment" => "operator %=",
            "op_BitwiseAndAssignment" => "operator &=",
            "op_BitwiseOrAssignment" => "operator |=",
            "op_ExclusiveOrAssignment" => "operator ^=",
            "op_LeftShiftAssignment" => "operator <<=",
            "op_RightShiftAssignment" => "operator >>=",
            "op_UnsignedRightShiftAssignment" => "operator >>>=",
            "op_IncrementAssignment" => "operator ++",
            "op_DecrementAssignment" => "operator --",
            "op_CheckedAdditionAssignment" => "operator checked +=",
            "op_CheckedSubtractionAssignment" => "operator checked -=",
            "op_CheckedMultiplicationAssignment" => "operator checked *=",
            "op_CheckedDivisionAssignment" => "operator checked /=",
            "op_CheckedIncrementAssignment" => "operator checked ++",
            "op_CheckedDecrementAssignment" => "operator checked --",
            "op_Implicit" => "implicit operator",
            "op_Explicit" => "explicit operator",
            "op_CheckedExplicit" => "explicit operator checked",
            _ => FormatMetadataIdentifier(metadataName),
        };

    public static string RemoveGenericArity(string name)
    {
        var index = name.IndexOf('`');
        return index < 0 ? name : name[..index];
    }
}
