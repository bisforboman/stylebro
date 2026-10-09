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

/// <summary>Fix for BRO1151: passes the array's elements to the params parameter.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParamsArrayCodeFixProvider))]
public sealed class ParamsArrayCodeFixProvider : NodeCodeFixProvider<ExpressionSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="ParamsArrayCodeFixProvider"/> class.</summary>
    public ParamsArrayCodeFixProvider()
        : base(DiagnosticIds.ParamsArray, "Pass the elements")
    {
    }

    /// <inheritdoc/>
    private protected override bool NeedsSemanticModel => true;

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(ExpressionSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        ParamsArrays.GetChanges(node, model!, cancellationToken);
}
