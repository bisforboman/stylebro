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

/// <summary>Fix for BRO1150 and Sonar's S2971 (same place: the 'Where' name): passes the 'Where' predicate to the call after it.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WhereBeforeTerminalCodeFixProvider))]
public sealed class WhereBeforeTerminalCodeFixProvider : NodeCodeFixProvider<InvocationExpressionSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="WhereBeforeTerminalCodeFixProvider"/> class.</summary>
    public WhereBeforeTerminalCodeFixProvider()
        : base(DiagnosticIds.WhereBeforeTerminal, "Pass the predicate instead of calling 'Where'", "S2971")
    {
    }

    /// <inheritdoc/>
    private protected override bool NeedsSemanticModel => true;

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(InvocationExpressionSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        WhereCalls.GetChanges(node, model!, options, cancellationToken)?.Changes;
}
