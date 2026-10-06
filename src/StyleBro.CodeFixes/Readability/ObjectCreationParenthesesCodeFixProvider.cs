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

/// <summary>Fix for BRO1141: removes or adds the empty parentheses.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ObjectCreationParenthesesCodeFixProvider))]
public sealed class ObjectCreationParenthesesCodeFixProvider : NodeCodeFixProvider<ObjectCreationExpressionSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="ObjectCreationParenthesesCodeFixProvider"/> class.</summary>
    public ObjectCreationParenthesesCodeFixProvider()
        : base(DiagnosticIds.ObjectCreationParentheses, "Write the parentheses in the configured style")
    {
    }

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(ObjectCreationExpressionSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        ObjectCreationParentheses.GetChange(node, options) is { } change ? new[] { change } : null;
}
