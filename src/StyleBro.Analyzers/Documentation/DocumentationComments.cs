using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1601 (StyleCop SA1600 for overrides and implementations: '/// &lt;inheritdoc/&gt;') and BRO1602
/// (SA1626: '///' used for a plain comment). Text-based, so BRO1602 works whether or not the project generates
/// documentation (without it, the compiler parses '///' as plain comments); BRO1601 asks for documentation only where
/// the project generates it.
/// </summary>
internal static class DocumentationComments
{
    /// <summary>Exposed elements (visible outside the assembly) need documentation: StyleCop's documentExposedElements.</summary>
    public const string ExposedElementsKey = "stylebro_document_exposed_elements";

    /// <summary>Elements visible only inside the assembly need documentation: StyleCop's documentInternalElements.</summary>
    public const string InternalElementsKey = "stylebro_document_internal_elements";

    /// <summary>Private elements (and members of private types) need documentation: StyleCop's documentPrivateElements.</summary>
    public const string PrivateElementsKey = "stylebro_document_private_elements";

    /// <summary>
    /// The SDK property that turns on documentation generation; the package's build targets make it compiler-visible. The
    /// SDK always sets it (true when DocumentationFile is set), so it's the same in a build and under 'dotnet format',
    /// which parses documentation comments even where the build doesn't.
    /// </summary>
    public const string GenerateDocumentationFileProperty = "build_property.GenerateDocumentationFile";

    /// <summary>The member declarations BRO1601 checks.</summary>
    public static readonly SyntaxKind[] MemberKinds =
    [
        SyntaxKind.MethodDeclaration,
        SyntaxKind.PropertyDeclaration,
        SyntaxKind.IndexerDeclaration,
        SyntaxKind.EventDeclaration,
        SyntaxKind.EventFieldDeclaration,
    ];

    /// <summary>Whether the declaration has a documentation comment ('///' or '/**') before it.</summary>
    public static bool HasDocumentation(SyntaxNode member)
    {
        return member.GetLeadingTrivia().Any(IsDocumentationComment);
    }

    /// <summary>
    /// Whether the project generates documentation: like StyleCop's documentation rules (SA0001), BRO1601 asks for none
    /// otherwise. From <see cref="GenerateDocumentationFileProperty"/>; without the package's build targets (an analyzer
    /// referenced directly), from whether the compiler parses documentation comments.
    /// </summary>
    public static bool GeneratesDocumentation(SyntaxTree tree, AnalyzerConfigOptions options)
    {
        return options.TryGetValue(GenerateDocumentationFileProperty, out var value) && value.Trim().Length > 0
            ? string.Equals(value.Trim(), "true", System.StringComparison.OrdinalIgnoreCase)
            : tree.Options.DocumentationMode >= DocumentationMode.Parse;
    }

    /// <summary>
    /// Whether an '#if', '#elif', '#else' or '#endif' sits above the member or between its attributes. Then the member
    /// may start with a different token in each target framework's copy ('[Obsolete]' only under '#if NET5_0_OR_GREATER'),
    /// and the '&lt;inheritdoc/&gt;' each copy wants lands in a different place: one copy then sees the other's as a
    /// '///' that documents nothing (BRO1602), and the two fixes never converge.
    /// </summary>
    public static bool HasConditionalDirective(MemberDeclarationSyntax member)
    {
        var end = member.AttributeLists.Count > 0 ? member.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart : member.SpanStart;
        return member.DescendantTrivia(TextSpan.FromBounds(member.FullSpan.Start, end)).Any(t =>
            t.IsKind(SyntaxKind.IfDirectiveTrivia) || t.IsKind(SyntaxKind.ElifDirectiveTrivia)
            || t.IsKind(SyntaxKind.ElseDirectiveTrivia) || t.IsKind(SyntaxKind.EndIfDirectiveTrivia));
    }

    /// <summary>
    /// Whether every symbol the declaration declares overrides a member or implements an interface member, so its
    /// documentation can come from there, and needs documentation at all: like StyleCop's SA1600, by the member's
    /// effective accessibility (a public member of an internal type is internal) and the three settings above
    /// (defaults: exposed and internal yes, private no). Explicit interface implementations ('void IDisposable.Dispose()')
    /// need none: they're only reachable through the interface, whose documentation tools show (like StyleCop's
    /// unreleased master, 2959cac8).
    /// </summary>
    public static bool InheritsDocumentation(IEnumerable<ISymbol> symbols, AnalyzerConfigOptions options)
    {
        var list = symbols.ToList();
        return list.Count > 0 && list.All(s =>
            !IsExplicitImplementation(s) && NeedsDocumentation(s, options) && (s.IsOverride || ImplementsInterfaceMember(s)));
    }

    /// <summary>
    /// The edit for BRO1601: '/// &lt;inheritdoc/&gt;' on its own line right before the member (after a plain comment
    /// or blank line before it, like StyleCop's fix), at the member's indentation.
    /// </summary>
    /// <remarks>
    /// With <paramref name="blankLineBefore"/> (BRO1513 is on), a blank line goes above the new comment when the line
    /// above holds code: neighbouring single-line properties may sit together (BRO1505), but a documented one may not,
    /// and without it 'dotnet format' needed a second run.
    /// </remarks>
    public static TextChange GetInheritDocChange(SyntaxNode member, SourceText text, bool blankLineBefore = false)
    {
        var line = text.Lines.GetLineFromPosition(member.SpanStart);
        var indentation = text.ToString(TextSpan.FromBounds(line.Start, member.SpanStart));
        var lineBreak = text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            lineBreak = "\n";
        }

        if (blankLineBefore && indentation.Trim().Length == 0 && Layout.DocumentationBlankLines.WantsBlankLineAbove(line, text))
        {
            return new TextChange(new TextSpan(line.Start, 0), lineBreak + indentation + "/// <inheritdoc/>" + lineBreak);
        }

        return new TextChange(new TextSpan(member.SpanStart, 0), "/// <inheritdoc/>" + lineBreak + indentation);
    }

    /// <summary>
    /// BRO1602: the '///' comments that don't document anything: not directly before a type, member or enum member.
    /// '////' (commented-out code) isn't a documentation comment.
    /// </summary>
    public static IEnumerable<SyntaxTrivia> GetMisplacedDocumentationComments(IEnumerable<SyntaxTrivia> trivia)
    {
        foreach (var comment in trivia)
        {
            if (IsTripleSlashComment(comment) && !DocumentsAMember(comment))
            {
                yield return comment;
            }
        }
    }

    /// <summary>Where BRO1602 is reported: the comment's first line, from its '///'.</summary>
    public static TextSpan GetReportSpan(SyntaxTrivia comment, SourceText text)
    {
        var line = text.Lines.GetLineFromPosition(comment.FullSpan.Start);
        return TextSpan.FromBounds(comment.FullSpan.Start, System.Math.Min(line.End, comment.FullSpan.End));
    }

    /// <summary>
    /// The edits for BRO1602: every '///' at the start of a line of the comment becomes '//', plus a space when text
    /// follows directly ('///note' -> '// note'), so the result also satisfies BRO1002.
    /// </summary>
    public static IEnumerable<TextChange> GetSlashChanges(SyntaxTrivia comment, SourceText text)
    {
        var first = text.Lines.GetLineFromPosition(comment.FullSpan.Start).LineNumber;
        var last = text.Lines.GetLineFromPosition(comment.Span.End).LineNumber;
        for (var number = first; number <= last; number++)
        {
            var line = text.Lines[number];
            var lineText = text.ToString(line.Span);
            var start = lineText.Length - lineText.TrimStart().Length;
            if (string.CompareOrdinal(lineText, start, "///", 0, 3) != 0 || line.Start + start < comment.FullSpan.Start)
            {
                continue;
            }

            var next = start + 3 < lineText.Length ? lineText[start + 3] : ' ';
            var replacement = char.IsWhiteSpace(next) ? "//" : "// ";
            yield return new TextChange(new TextSpan(line.Start + start, 3), replacement);
        }
    }

    private static bool NeedsDocumentation(ISymbol symbol, AnalyzerConfigOptions options)
    {
        var (key, defaultValue) = GetVisibility(symbol) switch
        {
            Accessibility.Public => (ExposedElementsKey, true),
            Accessibility.Internal => (InternalElementsKey, true),
            _ => (PrivateElementsKey, false),
        };
        return options.TryGetValue(key, out var value) && bool.TryParse(value.Trim(), out var configured) ? configured : defaultValue;
    }

    /// <summary>
    /// Public (visible outside the assembly, including protected), Internal or Private, from the member and every type
    /// around it. Explicit interface implementations count as public.
    /// </summary>
    private static Accessibility GetVisibility(ISymbol symbol)
    {
        var result = Accessibility.Public;
        for (var current = symbol; current is not null; current = current.ContainingType)
        {
            var accessibility = current is IMethodSymbol { MethodKind: MethodKind.ExplicitInterfaceImplementation }
                || current is IPropertySymbol { ExplicitInterfaceImplementations.Length: > 0 }
                || current is IEventSymbol { ExplicitInterfaceImplementations.Length: > 0 }
                    ? Accessibility.Public
                    : current.DeclaredAccessibility;
            var level = accessibility switch
            {
                Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal => Accessibility.Public,
                Accessibility.Internal or Accessibility.ProtectedAndInternal => Accessibility.Internal,
                _ => Accessibility.Private,
            };
            if (level < result)
            {
                result = level;
            }
        }

        return result;
    }

    private static bool IsDocumentationComment(SyntaxTrivia trivia)
    {
        return trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
            || IsTripleSlashComment(trivia);
    }

    private static bool IsTripleSlashComment(SyntaxTrivia trivia)
    {
        if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
        {
            return true;
        }

        if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
        {
            return false;
        }

        var text = trivia.ToString();
        return text.StartsWith("///", System.StringComparison.Ordinal) && !text.StartsWith("////", System.StringComparison.Ordinal);
    }

    /// <summary>Whether the comment is in the leading trivia of a type, member or enum member declaration.</summary>
    private static bool DocumentsAMember(SyntaxTrivia trivia)
    {
        var token = trivia.Token;
        if (!token.LeadingTrivia.Contains(trivia))
        {
            return false;
        }

        return token.Parent?.AncestorsAndSelf().FirstOrDefault(n => n.SpanStart == token.SpanStart && n is MemberDeclarationSyntax)
            is { } member && member is not BaseNamespaceDeclarationSyntax;
    }

    /// <summary>Implicit and explicit implementations alike: FindImplementationForInterfaceMember returns both.</summary>
    private static bool IsExplicitImplementation(ISymbol symbol) => symbol switch
    {
        IMethodSymbol method => method.ExplicitInterfaceImplementations.Length > 0,
        IPropertySymbol property => property.ExplicitInterfaceImplementations.Length > 0,
        IEventSymbol @event => @event.ExplicitInterfaceImplementations.Length > 0,
        _ => false,
    };

    private static bool ImplementsInterfaceMember(ISymbol symbol)
    {
        var type = symbol.ContainingType;
        return type is not null && type.AllInterfaces.Any(i => i.GetMembers().Any(m =>
            SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(m), symbol)));
    }
}
