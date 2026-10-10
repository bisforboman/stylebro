using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Naming;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// The rename behind BRO1312: one namespace part, in every declaration and reference of the whole solution (using
/// directives of every kind, qualified names, crefs), or nowhere. Computed on the original solution like the other
/// renames (<see cref="CamelCaseRenamer"/> applies the edits). Skipped, the warning staying, when the rename could
/// change or break something it can't edit or check:
/// - a string in the solution's own code (not generated code: the SDK's AssemblyInfo holds the assembly name), or an
///   additional file (Razor, XAML), contains the namespace's full name: type names in strings (Type.GetType,
///   serialized '$type', resource names);
/// - disabled '#if' code contains the part;
/// - a reference is in generated code (a source generator's or a designer file's output: the tool writes it again);
/// - another project's part of the namespace comes from an assembly outside the solution;
/// - the new name already means something: in the containing namespace (any project), at a reference that is looked
///   up by its simple name, or for code inside the containing namespace that uses the new name for something else
///   (it would then find the renamed namespace first).
/// </summary>
internal static class NamespaceRenamer
{
    public static async Task AddChangesAsync(
        Solution solution,
        IEnumerable<(string OldName, string NewName, Diagnostic Diagnostic)> namespaces,
        Dictionary<string, List<TextChange>> changes,
        CancellationToken cancellationToken)
    {
        foreach (var rename in namespaces.GroupBy(n => (n.OldName, n.NewName)))
        {
            var (edits, reason, where) = await GetChangesAsync(solution, rename.Key.OldName, rename.Key.NewName, cancellationToken).ConfigureAwait(false);
            if (reason is { } kept)
            {
                foreach (var item in rename)
                {
                    KeptFindings.Record(item.Diagnostic, kept, where);
                }

                continue;
            }

            foreach (var (file, change) in edits)
            {
                if (!changes.TryGetValue(file, out var list))
                {
                    changes[file] = list = new List<TextChange>();
                }

                list.Add(change);
            }
        }
    }

    /// <summary>The edits that rename the namespace part, or why it's kept.</summary>
    internal static async Task<(List<(string File, TextChange Change)> Changes, KeptReason? Reason, string? Where)> GetChangesAsync(
        Solution solution,
        string oldFullName,
        string newPart,
        CancellationToken cancellationToken)
    {
        var dot = oldFullName.LastIndexOf('.');
        var parentName = dot < 0 ? string.Empty : oldFullName.Substring(0, dot);
        var oldPart = oldFullName.Substring(dot + 1);
        var fullNameInText = new Regex(@"(?<!\w)" + Regex.Escape(oldFullName) + @"(?!\w)");
        var partInText = new Regex(@"(?<!\w)" + Regex.Escape(oldPart) + @"(?!\w)");
        var assemblies = new HashSet<string>(solution.Projects.Select(p => p.AssemblyName));

        var result = new List<(string File, TextChange Change)>();
        foreach (var project in solution.Projects)
        {
            foreach (var additional in project.AdditionalDocuments)
            {
                if (await additional.GetTextAsync(cancellationToken).ConfigureAwait(false) is { } text && fullNameInText.Match(text.ToString()) is { Success: true } match)
                {
                    return (result, KeptReason.NameInString, $"{additional.FilePath ?? additional.Name}({text.Lines.GetLineFromPosition(match.Index).LineNumber + 1})");
                }
            }

            if (await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false) is not { } compilation)
            {
                continue;
            }

            var global = compilation.GlobalNamespace;
            if (NamespaceNames.Find(global, oldFullName) is { } ns && !NamespaceNames.IsOnlyFrom(ns, a => assemblies.Contains(a.Name)))
            {
                return (result, KeptReason.NamespaceFromOutside, null);
            }

            if ((parentName.Length == 0 ? global : NamespaceNames.Find(global, parentName))?.GetMembers(newPart).Any() == true)
            {
                return (result, KeptReason.NewNameTaken, null);
            }

            // Source generators' trees too: a reference there can't be edited.
            foreach (var tree in compilation.SyntaxTrees)
            {
                var document = solution.GetDocument(tree);
                var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var model = compilation.GetSemanticModel(tree);

                // Strings in generated code are left out: the SDK's AssemblyInfo names the assembly ('AssemblyTitle("myCompany.data")'),
                // which usually is the root namespace. Generated code that uses the namespace is caught as a reference below.
                var generated = document is null or SourceGeneratedDocument || NamespaceNames.IsGenerated(tree);
                if (!generated && root.DescendantTokens().FirstOrDefault(t => (t.IsKind(SyntaxKind.StringLiteralToken) || t.IsKind(SyntaxKind.InterpolatedStringTextToken)
                        || t.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) || t.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
                        && fullNameInText.IsMatch(t.ValueText)) is { RawKind: not 0 } inString)
                {
                    return (result, KeptReason.NameInString, $"{tree.FilePath}({inString.GetLocation().GetLineSpan().StartLinePosition.Line + 1})");
                }

                if (root.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia) && partInText.IsMatch(t.ToString())))
                {
                    return (result, KeptReason.DisabledCode, null);
                }

                foreach (var name in root.DescendantNodes(descendIntoTrivia: true).OfType<IdentifierNameSyntax>())
                {
                    var text = name.Identifier.ValueText;
                    if (text == oldPart && NamespaceNames.GetNamespace(model, name)?.ToDisplayString() == oldFullName)
                    {
                        if (document is null || generated)
                        {
                            return (result, KeptReason.GeneratedReference, null);
                        }

                        if (IsLookedUp(name) && !model.LookupSymbols(name.SpanStart, name: newPart).IsEmpty)
                        {
                            return (result, KeptReason.NewNameMeansSomethingElse, null);
                        }

                        result.Add((CamelCaseRenamer.GetFileKey(document), new TextChange(name.Identifier.Span, newPart)));
                    }
                    else if (text == newPart && IsLookedUp(name) && SeesMembersOf(model, name, parentName)
                        && model.GetSymbolInfo(name).Symbol is null or INamespaceOrTypeSymbol or IAliasSymbol)
                    {
                        // 'Data' meaning a type from a using directive (or anything found outside the type): inside
                        // the containing namespace, the renamed namespace would be found first.
                        return (result, KeptReason.NewNameMeansSomethingElse, null);
                    }
                }
            }
        }

        return (result, null, null);
    }

    /// <summary>A name that is looked up in scope, as opposed to 'x.Name', 'A.Name', a declared namespace's name, 'Name = '.</summary>
    private static bool IsLookedUp(IdentifierNameSyntax name)
    {
        if (name.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>() is { } declaration && declaration.Name.Span.Contains(name.Span))
        {
            return false;
        }

        return name.Parent switch
        {
            QualifiedNameSyntax qualified when qualified.Right == name => false,
            AliasQualifiedNameSyntax alias when alias.Name == name => false,
            MemberAccessExpressionSyntax access when access.Name == name => false,
            MemberBindingExpressionSyntax or NameColonSyntax or NameEqualsSyntax => false,
            _ => true,
        };
    }

    /// <summary>Whether a lookup at <paramref name="name"/> reaches the members of the namespace <paramref name="parentName"/>.</summary>
    private static bool SeesMembersOf(SemanticModel model, IdentifierNameSyntax name, string parentName)
    {
        if (parentName.Length == 0)
        {
            return true;
        }

        var declaration = name.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault(d => !d.Name.Span.Contains(name.Span));
        var declared = declaration is null ? null : model.GetDeclaredSymbol(declaration)?.ToDisplayString();
        return declared is not null && (declared == parentName || declared.StartsWith(parentName + ".", System.StringComparison.Ordinal));
    }
}
