using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1144: identifiers that C# 14 reads as keywords, where that changes what the code means or breaks
/// the build (the cases in Roslyn's "breaking changes in C# 14" list, each checked with the C# 14 compiler). The fix
/// writes <c>@</c> before the identifier, which means the same in every language version, so a multi-targeted
/// project's older-language copies are unaffected.
/// </summary>
internal static class ContextualKeywords
{
    /// <summary>Whether the token is spelled like one of the identifiers BRO1144 looks at (cheap, syntax only).</summary>
    public static bool IsCandidate(SyntaxToken token) =>
        token.Span.Length is 5 or 7 or 9 && token.Text is "field" or "extension" or "partial";

    /// <summary>
    /// Whether the token should be written with <c>@</c>. A <c>field</c> token must come from a property's accessors or
    /// expression body, also from a lambda or local function there (CS9258, CS9272, CS9273); not from an indexer or
    /// event, where 'field' isn't a keyword. <paramref name="getModel"/> is only called for C# 14's <c>field</c> keyword
    /// (to see whether a member or local named <c>field</c> is what the code meant).
    /// </summary>
    public static bool ShouldEscape(SyntaxToken token, Func<SemanticModel?> getModel)
    {
        var parent = token.Parent;
        if (parent is null || parent.ContainsDiagnostics)
        {
            return false;
        }

        switch (token.Text)
        {
            case "field":
                return IsFieldName(token, parent, getModel);
            case "extension":
                return token.IsKind(SyntaxKind.IdentifierToken) && (NamesAType(token, parent) || StartsAMember(token, parent));
            case "partial":
                // 'partial F()' (a method or local function returning a type named 'partial') reads as a partial member.
                return parent is IdentifierNameSyntax name
                    && name.Parent switch
                    {
                        MethodDeclarationSyntax method => method.ReturnType == name,
                        LocalFunctionStatementSyntax local => local.ReturnType == name,
                        _ => false,
                    };
            default:
                return false;
        }
    }

    private static bool IsFieldName(SyntaxToken token, SyntaxNode parent, Func<SemanticModel?> getModel)
    {
        // Already C# 14: the keyword means the backing field. It still means the member or local of that name to whoever
        // wrote it when one exists (warning CS9258, whose suggested fix is '@field').
        // Only when that member or local has the property's type: otherwise '@field' wouldn't compile, and the keyword is
        // clearly meant (a string property next to an int field named 'field').
        if (CSharp14.IsFieldExpression(parent))
        {
            return getModel() is { } model
                && model.LookupSymbols(token.SpanStart, name: "field").FirstOrDefault() is { } symbol
                && SymbolEqualityComparer.Default.Equals(TypeOf(symbol), model.GetTypeInfo(parent).Type);
        }

        if (!token.IsKind(SyntaxKind.IdentifierToken))
        {
            return false;
        }

        // Before C# 14, a simple name 'field' that compiles refers to a member or local named 'field'. 'x.field',
        // 'this.field', named arguments and anonymous object members are names, not the keyword.
        if (parent is IdentifierNameSyntax name)
        {
            return name.Parent switch
            {
                MemberAccessExpressionSyntax access => access.Name != name,
                MemberBindingExpressionSyntax or NameColonSyntax or NameEqualsSyntax => false,
                QualifiedNameSyntax qualified => qualified.Right != name,
                AliasQualifiedNameSyntax alias => alias.Name != name,
                _ => true,
            };
        }

        // A local, parameter or range variable named 'field' declared there (error CS9272/CS9273 in C# 14).
        return parent is VariableDeclaratorSyntax or SingleVariableDesignationSyntax or ParameterSyntax or LocalFunctionStatementSyntax
            or ForEachStatementSyntax or CatchDeclarationSyntax or FromClauseSyntax or LetClauseSyntax or JoinClauseSyntax
            or JoinIntoClauseSyntax or QueryContinuationSyntax;
    }

    private static ITypeSymbol? TypeOf(ISymbol symbol) => symbol switch
    {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        ILocalSymbol local => local.Type,
        IParameterSymbol parameter => parameter.Type,
        _ => null,
    };

    /// <summary>A type, type parameter or alias named 'extension' (error CS9306 in C# 14).</summary>
    private static bool NamesAType(SyntaxToken token, SyntaxNode parent) => parent switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier == token,
        DelegateDeclarationSyntax @delegate => @delegate.Identifier == token,
        TypeParameterSyntax => true,
        IdentifierNameSyntax { Parent: NameEqualsSyntax { Parent: UsingDirectiveSyntax } } => true,
        _ => false,
    };

    /// <summary>
    /// The first token of a type's member after its attributes and modifiers: a field, property, method or operator of
    /// type 'extension', or a constructor of a type named 'extension'. C# 14 reads it as an extension block.
    /// </summary>
    private static bool StartsAMember(SyntaxToken token, SyntaxNode parent)
    {
        var member = parent.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (member?.Parent is not TypeDeclarationSyntax || member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
        {
            return false;
        }

        var first = member.Modifiers.Count > 0 ? member.Modifiers[member.Modifiers.Count - 1].GetNextToken()
            : member.AttributeLists.Count > 0 ? member.AttributeLists[member.AttributeLists.Count - 1].GetLastToken().GetNextToken()
            : member.GetFirstToken();
        return first == token;
    }
}
