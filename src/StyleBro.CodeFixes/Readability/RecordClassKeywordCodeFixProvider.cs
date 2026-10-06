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

/// <summary>Fix for BRO1146: removes 'class' from 'record class'.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RecordClassKeywordCodeFixProvider))]
public sealed class RecordClassKeywordCodeFixProvider : NodeCodeFixProvider<RecordDeclarationSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="RecordClassKeywordCodeFixProvider"/> class.</summary>
    public RecordClassKeywordCodeFixProvider()
        : base(DiagnosticIds.RecordClassKeyword, "Remove 'class'")
    {
    }

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(RecordDeclarationSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        RecordClassKeywords.GetChange(node) is { } change ? new[] { change } : null;
}
