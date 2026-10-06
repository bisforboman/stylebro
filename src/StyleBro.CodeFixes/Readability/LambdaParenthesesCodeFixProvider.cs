using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;

namespace StyleBro.CodeFixes.Readability;

/// <summary>Fix for BRO1136: removes the parentheses around a lambda's single parameter.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LambdaParenthesesCodeFixProvider))]
public sealed class LambdaParenthesesCodeFixProvider : NodeCodeFixProvider<ParenthesizedLambdaExpressionSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="LambdaParenthesesCodeFixProvider"/> class.</summary>
    public LambdaParenthesesCodeFixProvider()
        : base(DiagnosticIds.LambdaParentheses, "Remove the parentheses")
    {
    }

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(ParenthesizedLambdaExpressionSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        LambdaParentheses.GetChange(node) is { } change ? new[] { change } : null;
}
