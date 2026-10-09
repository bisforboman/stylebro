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

/// <summary>Fix for BRO1149: merges the inner 'if' into the enclosing one.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NestedIfCodeFixProvider))]
public sealed class NestedIfCodeFixProvider : NodeCodeFixProvider<IfStatementSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="NestedIfCodeFixProvider"/> class.</summary>
    public NestedIfCodeFixProvider()
        : base(DiagnosticIds.NestedIf, "Merge the 'if' statements")
    {
    }

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(IfStatementSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        NestedIfs.GetChange(node, text, options, isOn) is { } change ? new[] { change } : null;
}
