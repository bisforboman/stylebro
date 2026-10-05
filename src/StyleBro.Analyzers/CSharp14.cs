using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers;

/// <summary>
/// C# 14 syntax that the Roslyn StyleBro compiles against (4.8) doesn't know. The analyzers run inside newer compilers,
/// where these nodes exist with kinds 4.8's <c>SyntaxKind</c> has no name for, so they're recognized by their runtime
/// type. An extension block (<c>extension(string s) { ... }</c>) derives from <see cref="TypeDeclarationSyntax"/>, so its
/// members, braces and <c>WithMembers</c> work through that type.
/// </summary>
internal static class CSharp14
{
    /// <summary>Whether the node is a C# 14 extension block.</summary>
    public static bool IsExtensionBlock(SyntaxNode node) =>
        node is TypeDeclarationSyntax && node.GetType().Name == "ExtensionBlockDeclarationSyntax";

    /// <summary>Whether the node is C# 14's <c>field</c> keyword used as an expression (a property's backing field).</summary>
    public static bool IsFieldExpression(SyntaxNode? node) =>
        node is ExpressionSyntax && node.GetType().Name == "FieldExpressionSyntax";
}
