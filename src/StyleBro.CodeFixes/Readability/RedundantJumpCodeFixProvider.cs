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

/// <summary>Fix for BRO1137: removes the statement, its line and the blank lines above it.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RedundantJumpCodeFixProvider))]
public sealed class RedundantJumpCodeFixProvider : NodeCodeFixProvider<StatementSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="RedundantJumpCodeFixProvider"/> class.</summary>
    public RedundantJumpCodeFixProvider()
        : base(DiagnosticIds.RedundantJump, "Remove the redundant statement")
    {
    }

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(StatementSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        RedundantJumps.GetChange(node, text) is { } change ? new[] { change } : null;
}
