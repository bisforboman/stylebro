using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;

namespace StyleBro.CodeFixes.Layout;

/// <summary>Fix for BRO1527: puts the auto-accessors on the declaration's line.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AutoAccessorLinesCodeFixProvider))]
public sealed class AutoAccessorLinesCodeFixProvider : NodeCodeFixProvider<BasePropertyDeclarationSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="AutoAccessorLinesCodeFixProvider"/> class.</summary>
    public AutoAccessorLinesCodeFixProvider()
        : base(DiagnosticIds.AutoAccessorsOnOneLine, "Put the accessors on one line")
    {
    }

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(BasePropertyDeclarationSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        AutoAccessorLines.GetChanges(node, text, options, isOn);
}
