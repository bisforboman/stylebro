using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>Shared logic for BRO1408: <c>enum E : int</c> and <c>class C : object</c> say nothing the plain declaration doesn't.</summary>
internal static class RedundantBaseTypes
{
    /// <summary>
    /// The edit that removes the base type (with the colon when it is the only one, with the comma after it otherwise), or
    /// null when it isn't redundant or a comment or directive sits in the removed text. An enum's base is redundant when
    /// the underlying type is <see langword="int"/> however it is written (<see langword="int"/>, <c>Int32</c>, an alias); a class's when its
    /// first base type is named <see langword="object"/> or <c>Object</c> and binds to <c>System.Object</c>.
    /// </summary>
    public static TextChange? GetChange(BaseTypeDeclarationSyntax declaration, SemanticModel model, CancellationToken cancellationToken)
    {
        if (declaration.BaseList is not { } list || list.Types[0] is not SimpleBaseTypeSyntax first || declaration.ContainsDiagnostics)
        {
            return null;
        }

        var redundant = declaration is EnumDeclarationSyntax
            ? model.GetDeclaredSymbol(declaration, cancellationToken) is { EnumUnderlyingType.SpecialType: SpecialType.System_Int32 }
            : IsNamedObject(first.Type) && model.GetTypeInfo(first.Type, cancellationToken).Type?.SpecialType == SpecialType.System_Object;
        if (!redundant)
        {
            return null;
        }

        var span = list.Types.Count == 1
            ? TextSpan.FromBounds(list.ColonToken.GetPreviousToken().Span.End, list.Span.End)
            : TextSpan.FromBounds(first.SpanStart, list.Types[1].SpanStart);
        return Trivia.IsBlank(declaration, span) ? new TextChange(span, string.Empty) : null;
    }

    private static bool IsNamedObject(TypeSyntax type) => type switch
    {
        PredefinedTypeSyntax predefined => predefined.Keyword.IsKind(SyntaxKind.ObjectKeyword),
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText == "Object",
        AliasQualifiedNameSyntax aliased => aliased.Name.Identifier.ValueText == "Object",
        IdentifierNameSyntax name => name.Identifier.ValueText == "Object",
        _ => false,
    };
}
