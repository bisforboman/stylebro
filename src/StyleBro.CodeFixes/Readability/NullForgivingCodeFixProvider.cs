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

/// <summary>Fix for BRO1147: removes the redundant '!'.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullForgivingCodeFixProvider))]
public sealed class NullForgivingCodeFixProvider : NodeCodeFixProvider<PostfixUnaryExpressionSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="NullForgivingCodeFixProvider"/> class.</summary>
    public NullForgivingCodeFixProvider()
        : base(DiagnosticIds.RedundantNullForgiving, "Remove the redundant '!'")
    {
    }

    /// <inheritdoc/>
    private protected override bool NeedsSemanticModel => true;

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(PostfixUnaryExpressionSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        NullForgiving.GetChange(node, model!, options, cancellationToken) is { } change ? new[] { change } : null;
}
