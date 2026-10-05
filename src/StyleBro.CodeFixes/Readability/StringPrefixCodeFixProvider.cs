using System;
using System.Collections.Generic;
using System.Threading;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Readability;

/// <summary>Fix for BRO1138: writes the string literal in its simplest form.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringPrefixCodeFixProvider))]
public sealed class StringPrefixCodeFixProvider : NodeCodeFixProvider<ExpressionSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="StringPrefixCodeFixProvider"/> class.</summary>
    public StringPrefixCodeFixProvider()
        : base(DiagnosticIds.UnneededStringPrefix, "Simplify the string literal")
    {
    }

    /// <inheritdoc/>
    private protected override bool NeedsSemanticModel => true;

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(ExpressionSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        StringPrefixes.GetChange(node, model!, cancellationToken) is { } found ? new[] { found.Change } : null;
}
