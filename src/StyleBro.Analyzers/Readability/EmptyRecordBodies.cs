using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Modernize;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1140 (<c>record R(int X);</c> instead of <c>record R(int X) { }</c>) and BRO1145 (<c>class Marker;</c>
/// instead of <c>class Marker { }</c>, C# 12).
/// </summary>
internal static class EmptyRecordBodies
{
    /// <summary>The compiler-visible properties the package passes: the project's LangVersion and the SDK's default for this framework.</summary>
    public const string LangVersionProperty = "build_property.LangVersion";

    /// <inheritdoc cref="LangVersionProperty"/>
    public const string MaxSupportedLangVersionProperty = "build_property.MaxSupportedLangVersion";

    /// <summary>
    /// The edit that replaces the empty body (and a <c>;</c> after it, which BRO1101 would remove) with <c>;</c>, or null
    /// when the record has members, no parameter list, or anything but whitespace in the replaced text.
    /// </summary>
    public static TextChange? GetChange(RecordDeclarationSyntax record) =>
        record.ParameterList is null ? null : GetBodyChange(record);

    /// <summary>BRO1145: the same edit for a class, struct or interface, or null when the file's C# version is below 12.</summary>
    public static TextChange? GetTypeChange(TypeDeclarationSyntax type) =>
        ((CSharpParseOptions)type.SyntaxTree.Options).LanguageVersion >= LanguageVersion.CSharp12 ? GetBodyChange(type) : null;

    /// <summary>
    /// BRO1145's multi-target guard: in a project with several target frameworks, every copy of the file must compile
    /// as C# 12. That holds when every framework defaults to C# 12 or later (.NET 8+), or when the project sets
    /// LangVersion itself (then every framework uses it; a LangVersion that differs per framework isn't detected).
    /// Without the package's properties (a single framework as far as StyleBro knows) only the file's version counts.
    /// </summary>
    public static bool AllFrameworksAllowTypeBodySemicolon(AnalyzerConfigOptions global) =>
        MultiTargetSuppressor.AllFrameworksDefaultTo(global, new Version(8, 0));

    private static TextChange? GetBodyChange(TypeDeclarationSyntax type)
    {
        if (!type.OpenBraceToken.IsKind(SyntaxKind.OpenBraceToken) || type.Members.Count > 0 || type.ContainsDiagnostics)
        {
            return null;
        }

        var end = type.SemicolonToken.IsKind(SyntaxKind.SemicolonToken) ? type.SemicolonToken.Span.End : type.CloseBraceToken.Span.End;
        var span = TextSpan.FromBounds(type.OpenBraceToken.GetPreviousToken().Span.End, end);
        return Trivia.IsBlank(type, span) ? new TextChange(span, ";") : null;
    }
}
