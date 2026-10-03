using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>
/// BRO1404 (SA1400) access modifier declared, BRO1007 (SA1205) also on every part of a partial type. Which declarations
/// count follows StyleCop; the fix writes the accessibility the declaration has now (the default, or another part's).
/// </summary>
internal static class AccessModifiers
{
    public static readonly SyntaxKind[] Kinds =
    {
        SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration,
        SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration, SyntaxKind.EnumDeclaration,
        SyntaxKind.DelegateDeclaration, SyntaxKind.EventDeclaration, SyntaxKind.EventFieldDeclaration,
        SyntaxKind.MethodDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration,
        SyntaxKind.IndexerDeclaration, SyntaxKind.ConstructorDeclaration,
    };

    /// <summary>The rule, where the diagnostic goes, where the modifier goes and its text; null when nothing is missing.</summary>
    public static (string Id, SyntaxToken Location, int Position, string Modifier)? GetFinding(MemberDeclarationSyntax node, SemanticModel model, CancellationToken cancellationToken)
    {
        var modifiers = node.Modifiers;
        if (modifiers.Any(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword or SyntaxKind.PrivateKeyword)
            || modifiers.Any(m => m.IsKind(SyntaxKind.FileKeyword))
            || node.Parent is InterfaceDeclarationSyntax)
        {
            return null;
        }

        string id;
        SyntaxToken location;
        if (modifiers.Any(SyntaxKind.PartialKeyword))
        {
            // Partial methods and properties keep StyleCop's exception (another part may declare it); partial types are SA1205's.
            if (node is not TypeDeclarationSyntax type)
            {
                return null;
            }

            id = DiagnosticIds.PartialAccessModifier;
            location = type.Identifier;
        }
        else
        {
            location = node switch
            {
                BaseTypeDeclarationSyntax type => type.Identifier,
                DelegateDeclarationSyntax @delegate => @delegate.Identifier,
                EventDeclarationSyntax { ExplicitInterfaceSpecifier: null } @event => @event.Identifier,
                MethodDeclarationSyntax { ExplicitInterfaceSpecifier: null } method => method.Identifier,
                PropertyDeclarationSyntax { ExplicitInterfaceSpecifier: null } property => property.Identifier,
                IndexerDeclarationSyntax { ExplicitInterfaceSpecifier: null } indexer => indexer.ThisKeyword,
                BaseFieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault(v => !v.Identifier.IsMissing)?.Identifier ?? default,
                ConstructorDeclarationSyntax ctor when !modifiers.Any(SyntaxKind.StaticKeyword) => ctor.Identifier,
                _ => default,
            };
            id = DiagnosticIds.AccessModifier;
        }

        if (location.RawKind == 0 || location.IsMissing)
        {
            return null;
        }

        ISymbol? symbol = node is BaseFieldDeclarationSyntax f
            ? model.GetDeclaredSymbol(f.Declaration.Variables.First(v => v.Identifier == location), cancellationToken)
            : model.GetDeclaredSymbol(node, cancellationToken);
        var modifier = symbol?.DeclaredAccessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.Public => "public",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => null,
        };
        if (modifier is null || string.IsNullOrEmpty(symbol!.Name))
        {
            return null;
        }

        // Before the first modifier, or before the declaration's keyword or type: after its attributes.
        var first = node.AttributeLists.Count > 0 ? node.AttributeLists.Last().GetLastToken().GetNextToken() : node.GetFirstToken();
        return (id, location, first.SpanStart, modifier);
    }
}
