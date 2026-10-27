using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

    /// <summary>
    /// The SDK's dotnet_style_require_accessibility_modifiers: 'for_non_interface_members' (or not set) is StyleCop's
    /// SA1400/SA1205; 'always' also asks for them on interface members (C# 8 and later); 'never' and 'omit_if_default' ask
    /// for none (removing modifiers isn't what these rules do).
    /// </summary>
    public static (bool Required, bool InInterfaces) GetPreference(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue("dotnet_style_require_accessibility_modifiers", out var value))
        {
            return (true, false);
        }

        return value.Split(':')[0].Trim().ToLowerInvariant() switch
        {
            "always" => (true, true),
            "never" or "omit_if_default" => (false, false),
            _ => (true, false),
        };
    }

    /// <summary>The rule, where the diagnostic goes, where the modifier goes and its text; null when nothing is missing.</summary>
    public static (string Id, SyntaxToken Location, int Position, string Modifier)? GetFinding(
        MemberDeclarationSyntax node,
        SemanticModel model,
        CancellationToken cancellationToken,
        (bool Required, bool InInterfaces)? configured = null,
        LanguageVersion? languageVersion = null)
    {
        var preference = configured ?? (true, false);
        var modifiers = node.Modifiers;
        var inInterface = node.Parent is InterfaceDeclarationSyntax;
        if (!preference.Required
            || modifiers.Any(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword or SyntaxKind.PrivateKeyword)
            || modifiers.Any(m => m.IsKind(SyntaxKind.FileKeyword))
            || (inInterface && (!preference.InInterfaces || (languageVersion ?? ((CSharpParseOptions)node.SyntaxTree.Options).LanguageVersion) < LanguageVersion.CSharp8)))
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

        // A modifier in an '#if' branch this compilation skips ('#if !X' / 'file' / '#endif' / 'static class'): where the branch is
        // on, the declaration has it, and one added here would be 'file internal' (CS9052) or a second accessibility
        // (DapperAOT, trial 57: an embedded-resource template its tests compile with X defined).
        if (HasConditionalModifiers(node.GetFirstToken(), location))
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

        // A part of a file-local type: another part says 'file', and an access modifier here is CS9052.
        if (modifier is null || string.IsNullOrEmpty(symbol!.Name) || symbol is INamedTypeSymbol { IsFileLocal: true })
        {
            return null;
        }

        // Before the first modifier, or before the declaration's keyword or type: after its attributes.
        var first = node.AttributeLists.Count > 0 ? node.AttributeLists.Last().GetLastToken().GetNextToken() : node.GetFirstToken();
        return (id, location, first.SpanStart, modifier);
    }

    /// <summary>
    /// Whether the tokens from <paramref name="first"/> up to <paramref name="location"/> have a directive between them, or a
    /// skipped '#if' branch with nothing but modifiers before one of them (a branch around the whole declaration has more).
    /// </summary>
    private static bool HasConditionalModifiers(SyntaxToken first, SyntaxToken location)
    {
        for (var token = first; token.RawKind != 0 && token.SpanStart < location.SpanStart; token = token.GetNextToken())
        {
            if ((token != first && token.LeadingTrivia.Any(t => t.IsDirective))
                || token.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia) && IsOnlyModifiers(t.ToString())))
            {
                return true;
            }
        }

        return false;
    }

    // Whether a skipped branch's text is modifiers only ('file', 'public static').
    private static bool IsOnlyModifiers(string text)
    {
        var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 && words.All(w => (SyntaxFacts.GetKeywordKind(w) is var kind && kind != SyntaxKind.None ? kind : SyntaxFacts.GetContextualKeywordKind(w)) is
            SyntaxKind.PublicKeyword or SyntaxKind.PrivateKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword
            or SyntaxKind.FileKeyword or SyntaxKind.StaticKeyword or SyntaxKind.SealedKeyword or SyntaxKind.AbstractKeyword
            or SyntaxKind.PartialKeyword or SyntaxKind.ReadOnlyKeyword or SyntaxKind.UnsafeKeyword or SyntaxKind.NewKeyword
            or SyntaxKind.VirtualKeyword or SyntaxKind.OverrideKeyword or SyntaxKind.ExternKeyword or SyntaxKind.RefKeyword);
    }
}
