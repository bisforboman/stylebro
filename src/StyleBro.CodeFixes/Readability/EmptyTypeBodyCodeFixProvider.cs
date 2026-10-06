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

/// <summary>Fix for BRO1145: replaces the empty body of a class, struct or interface with ';'.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyTypeBodyCodeFixProvider))]
public sealed class EmptyTypeBodyCodeFixProvider : NodeCodeFixProvider<TypeDeclarationSyntax>
{
    /// <summary>Initializes a new instance of the <see cref="EmptyTypeBodyCodeFixProvider"/> class.</summary>
    public EmptyTypeBodyCodeFixProvider()
        : base(DiagnosticIds.EmptyTypeBody, "Replace the empty body with ';'")
    {
    }

    /// <inheritdoc/>
    private protected override IEnumerable<TextChange>? GetChanges(TypeDeclarationSyntax node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken) =>
        EmptyRecordBodies.GetTypeChange(node) is { } change ? new[] { change } : null;
}
