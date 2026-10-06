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

/// <summary>Fix for BRO1139: joins 'else' and 'if'.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ElseIfCodeFixProvider))]
public sealed class ElseIfCodeFixProvider : NodeCodeFixProvider<ElseClauseSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="ElseIfCodeFixProvider"/> class.</summary>
    public ElseIfCodeFixProvider()
        : base(DiagnosticIds.ElseIf, "Write 'else if'")
    {
    }

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(ElseClauseSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        ElseIfs.GetChanges(node, text, options, isOn);
}
