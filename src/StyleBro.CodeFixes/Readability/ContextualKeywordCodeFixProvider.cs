using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;

namespace StyleBro.CodeFixes.Readability;

/// <summary>Fix for BRO1144: writes '@' before the identifier.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ContextualKeywordCodeFixProvider))]
public sealed class ContextualKeywordCodeFixProvider : NodeCodeFixProvider<SyntaxNode>
{
    /// <summary>Initializes a new instance of the <see cref="ContextualKeywordCodeFixProvider"/> class.</summary>
    public ContextualKeywordCodeFixProvider()
        : base(DiagnosticIds.ContextualKeyword, "Escape the identifier with '@'")
    {
    }

    /// <inheritdoc/>
    private protected override bool NeedsSemanticModel => true;

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(SyntaxNode node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        node.ChildTokens()
            .Where(t => ContextualKeywords.IsCandidate(t) && ContextualKeywords.ShouldEscape(t, () => model))
            .Select(t => new TextChange(new TextSpan(t.SpanStart, 0), "@"));
}
