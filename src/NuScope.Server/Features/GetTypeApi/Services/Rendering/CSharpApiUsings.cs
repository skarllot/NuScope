using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Rendering;

internal sealed class CSharpApiUsings : CSharpSyntaxRewriter
{
    private static readonly Dictionary<string, SyntaxKind> Keywords = new(StringComparer.Ordinal)
    {
        ["System.Boolean"] = SyntaxKind.BoolKeyword,
        ["System.Byte"] = SyntaxKind.ByteKeyword,
        ["System.SByte"] = SyntaxKind.SByteKeyword,
        ["System.Char"] = SyntaxKind.CharKeyword,
        ["System.Decimal"] = SyntaxKind.DecimalKeyword,
        ["System.Double"] = SyntaxKind.DoubleKeyword,
        ["System.Single"] = SyntaxKind.FloatKeyword,
        ["System.Int16"] = SyntaxKind.ShortKeyword,
        ["System.UInt16"] = SyntaxKind.UShortKeyword,
        ["System.Int32"] = SyntaxKind.IntKeyword,
        ["System.UInt32"] = SyntaxKind.UIntKeyword,
        ["System.Int64"] = SyntaxKind.LongKeyword,
        ["System.UInt64"] = SyntaxKind.ULongKeyword,
        ["System.Object"] = SyntaxKind.ObjectKeyword,
        ["System.String"] = SyntaxKind.StringKeyword,
        ["System.Void"] = SyntaxKind.VoidKeyword,
    };

    private static readonly string[] Imports =
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
    ];

    private readonly HashSet<string> namespaces = new(StringComparer.Ordinal);

    public CSharpApiUsings(MetadataReader reader)
    {
        foreach (var handle in reader.TypeReferences)
        {
            AddNamespace(reader.GetString(reader.GetTypeReference(handle).Namespace));
        }

        foreach (var handle in reader.TypeDefinitions)
        {
            AddNamespace(reader.GetString(reader.GetTypeDefinition(handle).Namespace));
        }
    }

    public string Render(string declarations)
    {
        var root = SyntaxFactory.ParseCompilationUnit(declarations);
        var shortened = Visit(root)!.ToFullString();
        return string.Join(Environment.NewLine, Imports.Select(name => $"using {name};"))
            + Environment.NewLine
            + Environment.NewLine
            + shortened;
    }

    public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
    {
        if (node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Any(ns => ns.Name.Span.Contains(node.Span)))
        {
            return node;
        }

        if (GetKeywordType(node.ToString()) is { } keyword)
        {
            return keyword.WithTriviaFrom(node);
        }

        return CanShorten(node.Left.ToString(), node.ToString())
            ? Visit(node.Right)!.WithTriviaFrom(node)
            : base.VisitQualifiedName(node);
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node) =>
        GetKeywordType(node.ToString()) is { } keyword ? keyword.WithTriviaFrom(node)
        : CanShorten(node.Expression.ToString(), node.ToString()) ? Visit(node.Name)!.WithTriviaFrom(node)
        : base.VisitMemberAccessExpression(node);

    private static TypeSyntax? GetKeywordType(string name) =>
        Keywords.TryGetValue(name, out var keyword) ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(keyword))
        : name == "System.IntPtr" ? SyntaxFactory.IdentifierName("nint")
        : name == "System.UIntPtr" ? SyntaxFactory.IdentifierName("nuint")
        : null;

    private bool CanShorten(string prefix, string name) =>
        Array.IndexOf(Imports, prefix) >= 0 && !namespaces.Contains(name);

    private void AddNamespace(string name)
    {
        while (!string.IsNullOrEmpty(name))
        {
            namespaces.Add(name);
            var separator = name.LastIndexOf('.');
            name = separator < 0 ? "" : name[..separator];
        }
    }
}
