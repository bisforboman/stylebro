using System;
using System.Collections.Generic;
using System.Threading;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Maintainability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Maintainability;

/// <summary>Fix for BRO1408: removes the redundant base type.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RedundantBaseTypeCodeFixProvider))]
public sealed class RedundantBaseTypeCodeFixProvider : NodeCodeFixProvider<BaseTypeDeclarationSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="RedundantBaseTypeCodeFixProvider"/> class.</summary>
    public RedundantBaseTypeCodeFixProvider()
        : base(DiagnosticIds.RedundantBaseType, "Remove the redundant base type")
    {
    }

    /// <inheritdoc/>
    private protected override bool NeedsSemanticModel => true;

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(BaseTypeDeclarationSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        RedundantBaseTypes.GetChange(node, model!, cancellationToken) is { } change ? new[] { change } : null;
}
