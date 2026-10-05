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

/// <summary>Fix for BRO1140: replaces the empty body with ';'.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyRecordBodyCodeFixProvider))]
public sealed class EmptyRecordBodyCodeFixProvider : NodeCodeFixProvider<RecordDeclarationSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="EmptyRecordBodyCodeFixProvider"/> class.</summary>
    public EmptyRecordBodyCodeFixProvider()
        : base(DiagnosticIds.EmptyRecordBody, "Replace the empty body with ';'")
    {
    }

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(RecordDeclarationSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        EmptyRecordBodies.GetChange(node) is { } change ? new[] { change } : null;
}
