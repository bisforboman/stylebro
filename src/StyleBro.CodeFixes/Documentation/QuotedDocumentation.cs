using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StyleBro.CodeFixes.Naming;

namespace StyleBro.CodeFixes.Documentation;

/// <summary>
/// Documentation text the solution quotes. Trial 43, NSwag: BRO1603 put a period after a test controller's
/// '&lt;summary&gt;Test summary&lt;/summary&gt;', which NSwag reads at run time into the OpenAPI document, and a test comparing
/// it with "Test summary" failed. Owner's choice: the text-changing doc fixes (BRO1603-BRO1607) leave an element whose text is
/// a string literal, or is in a snapshot file, in the solution; the finding stays as a kept one.
/// </summary>
internal static class QuotedDocumentation
{
    private static readonly Regex Space = new(@"\s+", RegexOptions.CultureInvariant);

    /// <summary>Where the solution quotes the text of the documentation element at <paramref name="position"/>, or null.</summary>
    public static async Task<string?> FindAsync(Solution solution, SyntaxNode root, int position)
    {
        // A word or two ('Name', 'The id') is too common to tell a quote from a coincidence.
        if (GetElementText(root, position) is not { } text || text.IndexOf(' ') < 0)
        {
            return null;
        }

        var names = await CamelCaseRenamer.GetNamesInCodeAsync(solution).ConfigureAwait(false);
        if (names.Strings.Contains(text))
        {
            return names.WhereIs(text) ?? text;
        }

        return names.Files.Texts.FirstOrDefault(f => f.Text.IndexOf(text, StringComparison.Ordinal) >= 0).Path;
    }

    /// <summary>
    /// The text of the innermost documentation element around <paramref name="position"/>, its white space collapsed
    /// (as XML-doc readers return it), or null when it holds anything but text ('&lt;see cref="X"/&gt;').
    /// </summary>
    public static string? GetElementText(SyntaxNode root, int position)
    {
        var token = root.FindToken(position, findInsideTrivia: true);
        if (token.Parent?.AncestorsAndSelf().OfType<XmlElementSyntax>().FirstOrDefault() is not { } element
            || element.Content.Any(c => c is not XmlTextSyntax))
        {
            return null;
        }

        var text = new StringBuilder();
        foreach (var part in element.Content.OfType<XmlTextSyntax>().SelectMany(t => t.TextTokens))
        {
            text.Append(part.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) ? " " : part.ValueText);
        }

        var collapsed = Space.Replace(text.ToString(), " ").Trim();
        return collapsed.Length == 0 ? null : collapsed;
    }
}
