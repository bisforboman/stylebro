using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Naming;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// Renames tuple element names for BRO1311. Tuple element names are structural: every '(int count, ...)' in the source
/// is its own declaration, and a use like 't.count' binds to the element of whichever tuple type 't' has. Roslyn's
/// Renamer crashes on tuple elements and SymbolFinder matches elements of different tuple types by name, so a name is
/// renamed by declarations, solution-wide (every element in a tuple type with the old name is reported anyway):
/// <list type="number">
/// <item>every tuple type element with the old name, except in a member that overrides or implements a library member
/// whose own declaration has tuple names (they have to match; 'IComparer&lt;(int a, int b)&gt;.Compare' takes them from
/// the type argument, which is renamed too), and in a public signature unless stylebro_rename_public_api is true;</item>
/// <item>every explicit name in a tuple literal ('(count: 1, ...)') converted to a renamed element (else the compiler
/// warns that the name is ignored, CS8123); a literal that keeps its natural type isn't a tuple type the rule checks, and
/// keeps its names;</item>
/// <item>every use ('t.count', 't?.count', 'nameof', '(count: var c)' in a pattern) of an element declared at a renamed
/// place. Inferred names and elements from libraries keep theirs.</item>
/// </list>
/// A rename that adds compile errors anywhere, or a report of IDE0042 where that's a warning or an error
/// (<see cref="Deconstruction"/>), is dropped and its warning stays (a deliberate exception, like the naming rules'
/// reflection guard).
/// </summary>
internal static class TupleElementRenamer
{
    public static async Task<Solution> RenameAsync(Solution solution, IEnumerable<(string OldName, string NewName, Diagnostic Diagnostic)> renames, CancellationToken cancellationToken)
    {
        var items = renames.ToList();
        foreach (var group in await GroupAsync(solution, items.Select(r => (r.OldName, r.NewName)), cancellationToken).ConfigureAwait(false))
        {
            var (renamed, reason, blocker) = await TryRenameAllAsync(solution, group, cancellationToken).ConfigureAwait(false);
            if (renamed is not null)
            {
                solution = renamed;
                continue;
            }

            // The element that can't be renamed gives its reason; the others in its group say which one keeps them.
            foreach (var item in items.Where(r => group.Any(g => g.OldName == r.OldName)))
            {
                if (blocker is not null && item.OldName != blocker)
                {
                    KeptFindings.Record(item.Diagnostic, KeptReason.KeptWithOtherElement, $"'{blocker}': {KeptFinding.Describe(reason?.Reason ?? KeptReason.NothingToRename)}");
                }
                else
                {
                    KeptFindings.Record(item.Diagnostic, reason?.Reason ?? KeptReason.NothingToRename, reason?.Where);
                }
            }
        }

        return solution;
    }

    /// <summary>Why the fix leaves a rename on purpose, or null when it makes it (the same checks as <see cref="RenameAsync"/>).</summary>
    public static async Task<KeptReason?> GetKeptReasonAsync(Solution solution, string oldName, string newName, CancellationToken cancellationToken)
    {
        foreach (var group in await GroupAsync(solution, new[] { (oldName, newName) }, cancellationToken).ConfigureAwait(false))
        {
            if (group.Any(g => g.OldName == oldName))
            {
                var (_, reason, blocker) = await TryRenameAllAsync(solution, group, cancellationToken).ConfigureAwait(false);
                return blocker is not null && blocker != oldName ? KeptReason.KeptWithOtherElement : reason?.Reason;
            }
        }

        return KeptReason.NothingToRename;
    }

    /// <summary>
    /// The renames in groups that are made all or none: names that share a tuple type go together, with every other name
    /// in it that needs a new one ('(int total, int max)' with 'max' kept for IDE0042 became '(int Total, int max)',
    /// trial 20). A name the analyzer leaves (it's in disabled '#if' code) keeps its whole group (a null new name).
    /// </summary>
    private static async Task<List<List<(string OldName, string? NewName)>>> GroupAsync(Solution solution, IEnumerable<(string OldName, string NewName)> renames, CancellationToken cancellationToken)
    {
        var newNames = new Dictionary<string, string?>();
        foreach (var (oldName, newName) in renames)
        {
            newNames[oldName] = newName;
        }

        // Every tuple type's names (two or more), with the casing its file asks for.
        var tuples = new List<(string[] Names, bool CamelCase)>();
        foreach (var project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            foreach (var document in project.Documents)
            {
                if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
                {
                    continue;
                }

                var camelCase = TupleElementNames.IsCamelCase(project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
                foreach (var tuple in root.DescendantNodes().OfType<TupleTypeSyntax>())
                {
                    var names = tuple.Elements.Select(e => e.Identifier.ValueText).Where(n => n.Length > 0).ToArray();
                    if (names.Length > 1)
                    {
                        tuples.Add((names, camelCase));
                    }
                }
            }
        }

        Compilation?[]? compilations = null;
        var parents = newNames.Keys.ToDictionary(n => n, n => n);
        string Find(string name) => parents[name] == name ? name : parents[name] = Find(parents[name]);
        for (var changed = true; changed;)
        {
            changed = false;
            foreach (var (names, camelCase) in tuples.Where(t => t.Names.Any(newNames.ContainsKey)))
            {
                foreach (var name in names.Where(n => !newNames.ContainsKey(n)))
                {
                    if (TupleElementNames.GetNewName(name, camelCase) is { } partner)
                    {
                        compilations ??= await Task.WhenAll(solution.Projects.Select(p => p.GetCompilationAsync(cancellationToken))).ConfigureAwait(false);
                        newNames[name] = compilations.Any(c => c is not null && DisabledCode.Mentions(c, name)) ? null : partner;
                        parents[name] = name;
                        changed = true;
                    }
                }

                var inGroup = names.Where(newNames.ContainsKey).ToList();
                foreach (var name in inGroup.Skip(1))
                {
                    parents[Find(name)] = Find(inGroup[0]);
                }
            }
        }

        return newNames.GroupBy(n => Find(n.Key)).Select(g => g.Select(n => (n.Key, n.Value)).ToList()).ToList();
    }

    /// <summary>
    /// Renames the whole group, or none of it, why, and the name that can't be renamed (null when none does). A name with
    /// nothing to rename (only in public signatures, say) is skipped: its tuple types keep every name anyway, as the others
    /// are public there too.
    /// </summary>
    private static async Task<(Solution? Renamed, (KeptReason Reason, string? Where)? Reason, string? Blocker)> TryRenameAllAsync(Solution solution, List<(string OldName, string? NewName)> group, CancellationToken cancellationToken)
    {
        var any = false;
        foreach (var (oldName, newName) in group)
        {
            if (newName is null)
            {
                return (null, (KeptReason.DisabledCode, null), oldName);
            }

            var (renamed, reason) = await TryRenameAsync(solution, oldName, newName, cancellationToken).ConfigureAwait(false);
            if (renamed is not null)
            {
                (solution, any) = (renamed, true);
            }
            else if (reason?.Reason != KeptReason.NothingToRename)
            {
                return (null, reason, oldName);
            }
        }

        return any ? (solution, null, null) : (null, (KeptReason.NothingToRename, null), null);
    }

    private static async Task<(Solution? Renamed, (KeptReason Reason, string? Where)? Reason)> TryRenameAsync(Solution solution, string oldName, string newName, CancellationToken cancellationToken)
    {
        var (renamed, visible) = await RenameOneAsync(solution, oldName, newName, cancellationToken).ConfigureAwait(false);
        if (renamed is null)
        {
            return (null, (KeptReason.NothingToRename, null));
        }

        // Code this 'stylebro-migrate format' run doesn't load (RepositoryNames) can only see an element declared in a
        // signature other projects see (eShop's private 'FindUserFromExternalProviderAsync' returning '(ApplicationUser
        // user, ...)' was kept for a local 'user' in the MAUI app, outside the run), only from a project that sees the
        // member (an internal one: InternalsVisibleTo), and only uses it as a member ('.value', 'value:', another tuple
        // type's element): EntityGraphQL's internal '(bool IsSet, object? value)' was kept for a lambda's '(_, value) =>'
        // in an example project.
        if ((await CamelCaseRenamer.GetNamesInCodeAsync(solution).ConfigureAwait(false)).OutsideMembers.TryGetValue(oldName, out var files)
            && files.FirstOrDefault(file => visible.Any(member => CanSee(member, file))) is { } outside)
        {
            return (null, (KeptReason.UsedInProjectNotLoaded, outside));
        }

        return await AddsErrorsAsync(solution, renamed, cancellationToken).ConfigureAwait(false) ? (null, (KeptReason.AddsErrors, null))
            : await Deconstruction.AddsReportsAsync(solution, renamed, cancellationToken).ConfigureAwait(false) ? (null, (KeptReason.AddsDeconstruction, null))
            : (renamed, null);
    }

    /// <summary>
    /// Whether another project can see the element: it's in the signature of a member visible outside its type's
    /// project (not in a body, an initializer or a private member). <paramref name="member"/>: that member, or null when
    /// unknown (counts as public).
    /// </summary>
    private static bool IsVisibleToOtherProjects(TupleElementSyntax element, SemanticModel model, CancellationToken cancellationToken, out ISymbol? member)
    {
        member = null;
        if (element.Ancestors().Any(a => a is BlockSyntax or ArrowExpressionClauseSyntax or EqualsValueClauseSyntax))
        {
            return false;
        }

        var declaration = element.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
        member = declaration is BaseFieldDeclarationSyntax field ? model.GetDeclaredSymbol(field.Declaration.Variables[0], cancellationToken)
            : declaration is null ? null
            : model.GetDeclaredSymbol(declaration, cancellationToken);
        return member is null || CamelCaseRenamer.IsVisibleToOtherProjects(member);
    }

    /// <summary>
    /// Whether code in <paramref name="file"/> (a project this run didn't load) can see <paramref name="member"/>: an
    /// internal one only from an InternalsVisibleTo assembly, judged by the nearest project file's name (the default
    /// assembly name). A file without a project file above it counts.
    /// </summary>
    private static bool CanSee(ISymbol? member, string file)
    {
        if (member is null || !IsInternal(member))
        {
            return true;
        }

        // ponytail: the project file's name stands for the assembly name; a project with its own <AssemblyName> that an
        // InternalsVisibleTo names counts only when the names match.
        var project = FindProjectName(file);
        return project is null || member.ContainingAssembly.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "InternalsVisibleToAttribute" && a.ConstructorArguments.Length == 1)
            .Any(a => a.ConstructorArguments[0].Value is string name && string.Equals(name.Split(',')[0].Trim(), project, System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsInternal(ISymbol member)
    {
        for (var current = member; current is not null and not INamespaceSymbol; current = current.ContainingSymbol)
        {
            if (current.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)
            {
                return true;
            }
        }

        return false;
    }

    private static string? FindProjectName(string file)
    {
#pragma warning disable RS1035 // Only for files 'stylebro-migrate format' listed (RepositoryNames): the IDE and the build never get here.
        try
        {
            for (var directory = System.IO.Path.GetDirectoryName(file); !string.IsNullOrEmpty(directory); directory = System.IO.Path.GetDirectoryName(directory))
            {
                if (System.IO.Directory.EnumerateFiles(directory, "*.csproj").FirstOrDefault() is { } project)
                {
                    return System.IO.Path.GetFileNameWithoutExtension(project);
                }
            }
        }
        catch (System.Exception e) when (e is System.IO.IOException or System.UnauthorizedAccessException or System.ArgumentException)
        {
        }

        return null;
#pragma warning restore RS1035
    }

    private static async Task<(Solution? Renamed, List<ISymbol?> Visible)> RenameOneAsync(Solution solution, string oldName, string newName, CancellationToken cancellationToken)
    {
        var visible = new List<ISymbol?>();
        var documents = new List<(Document Document, SyntaxNode Root, SemanticModel Model)>();
        foreach (var document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is { } root
                && root.DescendantTokens().Any(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == oldName)
                && await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is { } model)
            {
                documents.Add((document, root, model));
            }
        }

        // Places whose element gets the new name (file|position of the name), and the edits per file.
        var renamed = new HashSet<string>();
        var changes = new Dictionary<string, List<TextChange>>();
        void Edit(Document document, TextChange change)
        {
            var path = document.FilePath ?? document.Name;
            if (!changes.TryGetValue(path, out var list))
            {
                changes[path] = list = new List<TextChange>();
            }

            list.Add(change);
        }

        void Rename(Document document, SyntaxToken token) => Edit(document, new TextChange(token.Span, newName));

        // 1. Tuple type elements.
        foreach (var (document, root, model) in documents)
        {
            foreach (var element in root.DescendantNodes().OfType<TupleElementSyntax>().Where(e => e.Identifier.ValueText == oldName))
            {
                if (!TupleElementNames.InheritsNames(element, model, cancellationToken)
                    && (PublicApi.IsRenameAllowed(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree))
                        || !TupleElementNames.IsInPublicSignature(element, model, cancellationToken)))
                {
                    renamed.Add(Key(element.Identifier.GetLocation()));
                    if (IsVisibleToOtherProjects(element, model, cancellationToken, out var member))
                    {
                        visible.Add(member);
                    }

                    Rename(document, element.Identifier);
                }
            }
        }

        // 2. Explicit names in tuple literals, when the element they end up in is renamed (or is their own).
        foreach (var (document, root, model) in documents)
        {
            foreach (var tuple in root.DescendantNodes().OfType<TupleExpressionSyntax>())
            {
                var converted = GetTargetType(tuple, model, cancellationToken) as INamedTypeSymbol;
                for (var i = 0; i < tuple.Arguments.Count; i++)
                {
                    if (tuple.Arguments[i].NameColon is not { } nameColon || nameColon.Name.Identifier.ValueText != oldName)
                    {
                        continue;
                    }

                    var target = converted is { IsTupleType: true } && i < converted.TupleElements.Length ? converted.TupleElements[i] : null;
                    if (target is not null && target.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))
                    {
                        Rename(document, nameColon.Name.Identifier);
                    }
                }
            }
        }

        // 3. Uses of renamed elements.
        foreach (var (document, root, model) in documents)
        {
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == oldName && IsElementUse(n)))
            {
                if (model.GetSymbolInfo(name, cancellationToken).Symbol is IFieldSymbol { ContainingType.IsTupleType: true } element
                    && element.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))
                {
                    // '(pair.first, ...)' and 'new { pair.first }' infer a name from the use: it's written out, so the
                    // inferred element (and every use of it) keeps the old name. Not in a literal converted to a tuple
                    // type, whose names win: there the written-out name was only a CS8123 (coverlet's 'return (false,
                    // map.First().documentName)', trial 20).
                    if (InferredNameStart(name, model, cancellationToken) is { } start)
                    {
                        Edit(document, new TextChange(new TextSpan(start.Position, 0), oldName + start.Separator));
                    }

                    Rename(document, name.Identifier);
                }
            }
        }

        if (changes.Count == 0)
        {
            return (null, visible);
        }

        foreach (var file in changes)
        {
            var copies = solution.Projects.SelectMany(p => p.Documents).Where(d => (d.FilePath ?? d.Name) == file.Key).ToList();
            var text = await copies[0].GetTextAsync(cancellationToken).ConfigureAwait(false);
            var merged = text.WithChanges(LinkedFileFixAllProvider.Merge(file.Value.Distinct().ToList()));
            foreach (var copy in copies)
            {
                solution = solution.WithDocumentText(copy.Id, merged);
            }
        }

        return (solution, visible);
    }

    /// <summary>
    /// The type a tuple literal is converted to, from its context: GetTypeInfo's ConvertedType (and the natural type) have
    /// elements that point at the literal's own names, so they can't tell a renamed target from none. Null where the
    /// context isn't one of these; the compile check catches a literal left behind (CS8123).
    /// </summary>
    private static ITypeSymbol? GetTargetType(TupleExpressionSyntax tuple, SemanticModel model, CancellationToken cancellationToken)
    {
        var operation = model.GetOperation(tuple, cancellationToken);
        while (operation?.Parent is IConversionOperation or IParenthesizedOperation)
        {
            operation = operation.Parent;
        }

        return operation?.Parent switch
        {
            IConversionOperation conversion => conversion.Type,
            ISimpleAssignmentOperation assignment => assignment.Target.Type,
            IVariableInitializerOperation { Parent: IVariableDeclaratorOperation declarator } => declarator.Symbol.Type,
            IFieldInitializerOperation field => field.InitializedFields.FirstOrDefault()?.Type,
            IPropertyInitializerOperation property => property.InitializedProperties.FirstOrDefault()?.Type,
            IArgumentOperation argument => argument.Parameter?.Type,
            IReturnOperation => ReturnType(model.GetEnclosingSymbol(tuple.SpanStart, cancellationToken) as IMethodSymbol),
            _ => null,
        };
    }

    private static ITypeSymbol? ReturnType(IMethodSymbol? method) =>
        method is { IsAsync: true, ReturnType: INamedTypeSymbol { TypeArguments.Length: 1 } task } ? task.TypeArguments[0] : method?.ReturnType;

    /// <summary>
    /// Where a tuple argument or anonymous object member gets its name from this use ('(pair.first, ...)', 'new {
    /// pair.first }'), and what goes after the written-out name; null when the use doesn't name anything.
    /// </summary>
    private static (int Position, string Separator)? InferredNameStart(IdentifierNameSyntax name, SemanticModel model, CancellationToken cancellationToken)
    {
        SyntaxNode expression = name;
        while (expression.Parent is ExpressionSyntax parent and not TupleExpressionSyntax && parent.GetLastToken() == name.Identifier)
        {
            expression = parent;
        }

        return expression.Parent switch
        {
            ArgumentSyntax { NameColon: null, Parent: TupleExpressionSyntax tuple } argument
                when model.GetOperation(tuple, cancellationToken)?.Parent is not IConversionOperation { Type: INamedTypeSymbol { IsTupleType: true } } => (argument.SpanStart, ": "),
            AnonymousObjectMemberDeclaratorSyntax { NameEquals: null } member => (member.SpanStart, " = "),
            _ => null,
        };
    }

    /// <summary>A name that refers to a tuple element: after '.' or '?.', or the name in a positional pattern.</summary>
    private static bool IsElementUse(IdentifierNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Name == name,
        MemberBindingExpressionSyntax => true,
        NameColonSyntax { Parent: SubpatternSyntax } => true,
        _ => false,
    };

    private static string Key(Location location) => $"{location.SourceTree?.FilePath}|{location.SourceSpan.Start}";

    /// <summary>
    /// Whether any project with a changed document has more compile errors afterwards, or more CS8123 warnings (a tuple
    /// literal's name ignored because the type it converts to was renamed and the literal wasn't).
    /// </summary>
    private static async Task<bool> AddsErrorsAsync(Solution before, Solution after, CancellationToken cancellationToken)
    {
        foreach (var projectId in after.GetChanges(before).GetProjectChanges().Select(c => c.ProjectId))
        {
            var old = await before.GetProject(projectId)!.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var updated = await after.GetProject(projectId)!.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (Errors(updated) > Errors(old))
            {
                return true;
            }
        }

        return false;
    }

    private static int Errors(Compilation? compilation) =>
        compilation?.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error || d.Id == "CS8123") ?? 0;
}
