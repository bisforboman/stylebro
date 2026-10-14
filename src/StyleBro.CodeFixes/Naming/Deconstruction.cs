using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StyleBro.Analyzers;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// The SDK's IDE0042 ('variable declaration can be deconstructed') leaves a tuple local alone while one of its element
/// names is also the name of something in scope (sharpcompress: '(int returnCode, huftNode huftPointer, int e)' next to a
/// local 'huftPointer'). A rename can end that clash, from either side (BRO1311 renames the element, BRO1301 the local,
/// BRO1303 a field), and IDE0042 then reports the local: a build break where the repository sets it to error. IDE rules
/// don't run in the fix's compilation, so this mirrors IDE0042's check (Roslyn's CSharpUseDeconstructionDiagnosticAnalyzer)
/// and a rename that adds a report is dropped, when IDE0042 is a warning or an error for that file.
/// </summary>
internal static class Deconstruction
{
    private const string Id = "IDE0042";

    /// <summary>Whether IDE0042 reports more places in <paramref name="after"/>'s changed documents than before.</summary>
    // ponytail: only changed documents are checked; a renamed member that ends a clash in a file not referencing it
    // (another partial part, a derived type) isn't seen. Check the type's other files if that shows up.
    public static async Task<bool> AddsReportsAsync(Solution before, Solution after, CancellationToken cancellationToken)
    {
        foreach (var project in after.GetChanges(before).GetProjectChanges())
        {
            foreach (var id in project.GetChangedDocuments())
            {
                var old = before.GetDocument(id);
                var updated = after.GetDocument(id);
                if (old is null || updated is null
                    || await updated.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false) is not { } tree
                    || !IsEnforced(updated.Project, tree, cancellationToken))
                {
                    continue;
                }

                if (await CountAsync(updated, cancellationToken).ConfigureAwait(false) > await CountAsync(old, cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Whether IDE0042 is on as a warning or an error for the file (what breaks a build or a verify run).</summary>
    private static bool IsEnforced(Project project, SyntaxTree tree, CancellationToken cancellationToken)
    {
        var options = project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
        options.TryGetValue("csharp_style_deconstructed_variable_declaration", out var style);
        if (style?.Trim().StartsWith("false", System.StringComparison.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        return Severities.Configured(project.CompilationOptions, tree, Id, cancellationToken) is { } severity
            ? severity is ReportDiagnostic.Warn or ReportDiagnostic.Error
            : style is not null && (style.Contains(":warning") || style.Contains(":error"));
    }

    private static async Task<int> CountAsync(Document document, CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
            || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model)
        {
            return 0;
        }

        var count = 0;
        foreach (var node in root.DescendantNodes())
        {
            if (node is VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax { Parent: { } parent }, Variables.Count: 1 } declaration
                && declaration.Variables[0] is { Initializer.Value: { } value } declarator
                && IsViable(declaration.Type)
                && model.GetDeclaredSymbol(declarator, cancellationToken) is ILocalSymbol local)
            {
                var scope = parent is GlobalStatementSyntax global ? global.Parent! : parent;
                count += IsReported(model, local, declaration.Type, model.GetConversion(value, cancellationToken), scope, cancellationToken) ? 1 : 0;
            }
            else if (node is ForEachStatementSyntax forEach
                && IsViable(forEach.Type)
                && model.GetDeclaredSymbol(forEach, cancellationToken) is { } element)
            {
                count += IsReported(model, element, forEach.Type, model.GetForEachStatementInfo(forEach).ElementConversion, forEach, cancellationToken) ? 1 : 0;
            }
        }

        return count;
    }

    /// <summary>'var t' or a tuple type with every element named.</summary>
    private static bool IsViable(TypeSyntax type) =>
        type.IsVar || (type is TupleTypeSyntax tuple && tuple.Elements.All(e => e.Identifier.IsKind(SyntaxKind.IdentifierToken)));

    private static bool IsReported(SemanticModel model, ILocalSymbol local, TypeSyntax type, Conversion conversion, SyntaxNode scope, CancellationToken cancellationToken)
    {
        if (conversion is { Exists: true, IsIdentity: false, IsTupleConversion: false, IsTupleLiteralConversion: false }
            || model.GetTypeInfo(type, cancellationToken).Type is not INamedTypeSymbol { IsTupleType: true, TupleElements.Length: >= 2 } tuple
            || tuple.TupleElements.Any(e => e.IsImplicitlyDeclared))
        {
            return false;
        }

        // Only used to read named elements ('t.count'), never the tuple itself or 'Item1'.
        foreach (var name in scope.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (name.Identifier.ValueText != local.Name || !SymbolEqualityComparer.Default.Equals(local, model.GetSymbolInfo(name, cancellationToken).Symbol))
            {
                continue;
            }

            if (name.Parent is not MemberAccessExpressionSyntax access)
            {
                return false;
            }

            if (model.GetSymbolInfo(access, cancellationToken).Symbol is not IFieldSymbol { IsImplicitlyDeclared: false })
            {
                return false;
            }
        }

        // The clash: an element named like something in scope or declared in the scope.
        var reserved = new HashSet<string>(model.LookupSymbols(scope.SpanStart).Select(s => s.Name));
        foreach (var node in scope.DescendantNodesAndSelf())
        {
            if (model.GetDeclaredSymbol(node, cancellationToken) is { } declared)
            {
                reserved.Add(declared.Name);
            }
        }

        return !tuple.TupleElements.Any(e => reserved.Contains(e.Name));
    }
}
