using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Naming;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// Renames the variables, parameters and fields reported by BRO1301/BRO1302/BRO1303, for the single fix and Fix All alike. All renames
/// are computed on the original solution and applied at once, as text edits of the declaration and every reference
/// (named arguments and '&lt;param&gt;' docs included). A parameter's rename cascades to the parameters of overrides
/// and implementations that have the same name, so they don't drift apart. Linked files (multi-targeting) are handled
/// like in <see cref="LinkedFileFixAllProvider"/>: the edits of every copy are merged per physical file.
/// </summary>
internal static class CamelCaseRenamer
{
    private static readonly ConditionalWeakTable<Solution, Task<NamesInCode>> NamesCache = new();

    /// <summary>
    /// Tests only: counts the solution scans of <see cref="GetNamesInCodeAsync"/> made in this async flow while set (a fix
    /// that asks with a changed document's solution scans once per file instead of once per Fix All).
    /// </summary>
    internal static readonly AsyncLocal<StrongBox<int>?> NameScans = new();

    private static readonly ConditionalWeakTable<Solution, Solution> StrippedCache = new();

    private static readonly object RepositoryNamesGate = new();

    private static (string? File, List<RepositoryNames.Entry> Entries) repositoryNames = (null, new List<RepositoryNames.Entry>());

    public static async Task<Solution> RenameAsync(
        Solution solution,
        IEnumerable<(Document Document, Diagnostic Diagnostic)> items,
        CancellationToken cancellationToken)
    {
        var original = solution;
        solution = WithoutUnresolvedAnalyzers(solution);
        var changes = new Dictionary<string, List<TextChange>>();
        var done = new HashSet<(string File, int Start)>();
        var namespaces = new List<(string, string, Diagnostic)>();
        var renames = new List<(Document Document, Diagnostic Diagnostic)>();
        foreach (var (document, diagnostic) in items)
        {
            if (diagnostic.Id == DiagnosticIds.NamespacePascalCase)
            {
                if (GetNamespaceRename(diagnostic) is { } rename)
                {
                    namespaces.Add((rename.OldName, rename.NewName, diagnostic));
                }

                continue;
            }

            if (diagnostic.Properties.ContainsKey(CamelCaseNamingAnalyzer.NewNameKey)
                && done.Add((GetFileKey(document), diagnostic.Location.SourceSpan.Start))
                && solution.GetDocument(document.Id) is { } work)
            {
                renames.Add((work, diagnostic));
            }
        }

        // Every rename is computed on the original solution, so they run in parallel (FindReferencesAsync in every copy of
        // a multi-targeted file was most of BRO1303's Fix All); the results are used in the diagnostics' order.
        var keptTypes = new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);
        var results = await LinkedFileFixAllProvider.ForEachAsync(renames, item => GetCheckedChangesAsync(solution, item.Document, item.Diagnostic, keptTypes, cancellationToken), cancellationToken).ConfigureAwait(false);
        for (var i = 0; i < renames.Count; i++)
        {
            var (itemChanges, reason, where) = results[i];
            if (reason is { } kept)
            {
                KeptFindings.Record(renames[i].Diagnostic, kept, where);
                continue;
            }

            foreach (var (file, change) in itemChanges)
            {
                if (!changes.TryGetValue(file, out var list))
                {
                    changes[file] = list = new List<TextChange>();
                }

                list.Add(change);
            }
        }

        await NamespaceRenamer.AddChangesAsync(solution, namespaces, changes, cancellationToken).ConfigureAwait(false);
        return await ApplyAsync(original, changes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Why the fix leaves the diagnostic on purpose, or null when it renames it: the same checks as
    /// <see cref="RenameAsync"/>. The analyzer doesn't report what it can see is unsafe; these are the reasons it can't
    /// see (strings, nameof and overrides in other projects, generated code).
    /// </summary>
    public static async Task<KeptReason?> GetKeptReasonAsync(Solution solution, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        solution = WithoutUnresolvedAnalyzers(solution);
        document = solution.GetDocument(document.Id) ?? document;
        if (diagnostic.Id == DiagnosticIds.NamespacePascalCase)
        {
            return GetNamespaceRename(diagnostic) is { } rename
                ? (await NamespaceRenamer.GetChangesAsync(solution, rename.OldName, rename.NewName, cancellationToken).ConfigureAwait(false)).Reason
                : null;
        }

        var keptTypes = new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);
        return (await GetCheckedChangesAsync(solution, document, diagnostic, keptTypes, cancellationToken).ConfigureAwait(false)).Reason;
    }

    /// <summary>
    /// <see cref="GetItemChangesAsync"/> once per solution and diagnostic (<see cref="RenameChecks"/>): the registration's
    /// check and Fix All's are the same question.
    /// </summary>
    private static Task<(List<(string File, TextChange Change)> Changes, KeptReason? Reason, string? Where)> GetCheckedChangesAsync(
        Solution solution,
        Document document,
        Diagnostic diagnostic,
        ConcurrentDictionary<ISymbol, bool> keptTypes,
        CancellationToken cancellationToken)
    {
        diagnostic.Properties.TryGetValue(CamelCaseNamingAnalyzer.NewNameKey, out var newName);
        var key = $"{nameof(CamelCaseRenamer)}|{document.Id.Id}|{diagnostic.Id}|{diagnostic.Location.SourceSpan}|{newName}";
        return RenameChecks.GetAsync(solution, key, ct => GetItemChangesAsync(solution, document, diagnostic, keptTypes, ct), cancellationToken);
    }

    /// <summary>
    /// The solution without analyzer references that couldn't be loaded, cached per solution. An analyzer
    /// <c>ProjectReference</c> that isn't built yet (akka.net's source generators before a build) is an
    /// <c>UnresolvedAnalyzerReference</c>, and the SDK's Roslyn throws on it in every SymbolFinder call ("Unexpected
    /// value", while computing the project's checksum), so no rename was possible. It contributes nothing to the
    /// compilation, so the renames are computed without it and applied to the original solution as text.
    /// </summary>
    internal static Solution WithoutUnresolvedAnalyzers(Solution solution)
    {
        lock (StrippedCache)
        {
            return StrippedCache.GetValue(solution, s =>
            {
                foreach (var project in s.Projects)
                {
                    if (project.AnalyzerReferences.Any(r => r is UnresolvedAnalyzerReference))
                    {
                        s = s.WithProjectAnalyzerReferences(project.Id, project.AnalyzerReferences.Where(r => r is not UnresolvedAnalyzerReference));
                    }
                }

                return s;
            });
        }
    }

    /// <summary>The string literals, nameof names and reads by position in the solution, collected once per solution.</summary>
    internal static Task<NamesInCode> GetNamesInCodeAsync(Solution solution)
    {
        // Locked: the table may run the factory once per thread that asks at the same time (parallel renames).
        lock (NamesCache)
        {
            return NamesCache.GetValue(WithoutUnresolvedAnalyzers(solution), s =>
            {
                if (NameScans.Value is { } scans)
                {
                    Interlocked.Increment(ref scans.Value);
                }

                return CollectNamesAsync(s, CancellationToken.None);
            });
        }
    }

    /// <summary>
    /// A physical file and its text: the copies of a multi-targeted file share edits only while their text is the same
    /// ('dotnet format' runs its whitespace and code style fixes first and can leave the copies different; edits found in
    /// one copy don't fit the other's text). See <see cref="LinkedFileFixAllProvider"/>.
    /// </summary>
    internal static string GetFileKey(Document document)
    {
        var text = document.TryGetText(out var loaded) ? loaded : document.GetTextAsync().GetAwaiter().GetResult();
        return (document.FilePath ?? document.Id.Id.ToString()) + "|" + Convert.ToBase64String(text.GetChecksum().ToArray());
    }

    /// <summary>
    /// The entries of the file <see cref="RepositoryNames.Variable"/> names (read once per file), or none when it isn't set:
    /// only 'bro format' sets it.
    /// </summary>
    internal static List<RepositoryNames.Entry> ReadRepositoryNames()
    {
#pragma warning disable RS1035 // Only 'bro format' sets the variable; the IDE and the build never read a file.
        var file = Environment.GetEnvironmentVariable(RepositoryNames.Variable);
        if (string.IsNullOrEmpty(file))
        {
            return new List<RepositoryNames.Entry>();
        }

        lock (RepositoryNamesGate)
        {
            if (repositoryNames.File != file)
            {
                List<RepositoryNames.Entry> entries;
                try
                {
                    entries = System.IO.File.ReadLines(file).Select(RepositoryNames.Parse).OfType<RepositoryNames.Entry>().ToList();
                }
                catch (System.IO.IOException)
                {
                    entries = new List<RepositoryNames.Entry>();
                }

                repositoryNames = (file, entries);
            }

            return repositoryNames.Entries;
        }
#pragma warning restore RS1035
    }

    /// <summary>'obj/Debug/net8.0/App.AssemblyInfo.cs' and the like: assembly attributes MSBuild writes.</summary>
    internal static bool IsBuildMetadataFile(string? path)
    {
        if (path is null)
        {
            return false;
        }

        var name = System.IO.Path.GetFileName(path);
        return (name.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".AssemblyAttributes.cs", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase))
            && path.Replace('\\', '/').IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Whether code in another project can use the symbol by name: not private (internal counts, InternalsVisibleTo), nor
    /// in a private type; a parameter by its member (named arguments). Locals and local functions can't.
    /// </summary>
    internal static bool IsVisibleToOtherProjects(ISymbol symbol)
    {
        var current = symbol is IParameterSymbol or ITypeParameterSymbol ? symbol.ContainingSymbol : symbol;
        for (; current is not null and not INamespaceSymbol; current = current.ContainingSymbol)
        {
            if (current.DeclaredAccessibility is Accessibility.Private or Accessibility.NotApplicable)
            {
                return false;
            }
        }

        return current is not null;
    }

    /// <summary>
    /// The interpolated string handler type when the span is inside an interpolation hole whose handler's AppendFormatted
    /// takes the hole's text as a <c>[CallerArgumentExpression]</c> argument (owner's choice 2026-10-17, trial 28: ZLogger
    /// logs <c>$"{userId}"</c> under the key "userId", so a rename changes the logged data); else null. Syntax first: only
    /// spans inside an interpolation are bound, once per hole.
    /// </summary>
    internal static async Task<string?> GetLogKeyHandlerAsync(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
        {
            return null;
        }

        var node = root.FindToken(span.Start).Parent;
        return node?.FirstAncestorOrSelf<InterpolationSyntax>() is null
            || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model
            ? null
            : GetLogKeyHandler(node, span, model, cancellationToken);
    }

    /// <summary><see cref="GetLogKeyHandlerAsync"/> from the node at the span, with its semantic model.</summary>
    internal static string? GetLogKeyHandler(SyntaxNode node, TextSpan span, SemanticModel model, CancellationToken cancellationToken)
    {
        for (var current = node; current is not null and not MemberDeclarationSyntax; current = current.Parent)
        {
            if (current is InterpolationSyntax interpolation
                && interpolation.Expression.Span.Contains(span)
                && model.GetOperation(interpolation.Parent!, cancellationToken) is IInterpolatedStringOperation content
                && content.Parts.FirstOrDefault(p => p.Syntax == interpolation) is IInterpolatedStringAppendOperation { AppendCall: IInvocationOperation { TargetMethod: { Parameters: { Length: > 1 } parameters } method } }
                && parameters.Any(p => p.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "CallerArgumentExpressionAttribute"
                    && a.ConstructorArguments.Length == 1
                    && a.ConstructorArguments[0].Value as string == parameters[0].Name)))
            {
                return method.ContainingType.Name;
            }
        }

        return null;
    }

    /// <summary>The solution with the edits, keyed by <see cref="GetFileKey"/>.</summary>
    private static async Task<Solution> ApplyAsync(Solution solution, Dictionary<string, List<TextChange>> changes, CancellationToken cancellationToken)
    {
        var documentsByKey = solution.Projects.SelectMany(p => p.Documents).ToLookup(GetFileKey);
        var documentsByPath = solution.Projects.SelectMany(p => p.Documents).ToLookup(d => d.FilePath ?? d.Id.Id.ToString());
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in changes)
        {
            // Copies of one file whose text differs: every copy gets the first variant's text with its renames (see
            // LinkedFileFixAllProvider); the other variants' edits don't fit that text.
            var path = file.Key.Substring(0, file.Key.LastIndexOf('|'));
            if (!paths.Add(path))
            {
                continue;
            }

            var documents = documentsByKey[file.Key].ToList();
            if (documents.Count == 0)
            {
                continue;
            }

            var original = await documents[0].GetTextAsync(cancellationToken).ConfigureAwait(false);
            var merged = original.WithChanges(LinkedFileFixAllProvider.Merge(file.Value));
            foreach (var document in documentsByPath[path])
            {
                solution = solution.WithDocumentText(document.Id, merged);
            }
        }

        return solution;
    }

    private static (string OldName, string NewName)? GetNamespaceRename(Diagnostic diagnostic) =>
        diagnostic.Properties.TryGetValue(NamespaceNames.NamespaceKey, out var oldNamespace) && oldNamespace is not null
        && diagnostic.Properties.TryGetValue(CamelCaseNamingAnalyzer.NewNameKey, out var newPart) && newPart is not null
            ? (oldNamespace, newPart)
            : null;

    /// <summary>The edits for one diagnostic, in every copy of its file, or why it's kept.</summary>
    private static async Task<(List<(string File, TextChange Change)> Changes, KeptReason? Reason, string? Where)> GetItemChangesAsync(
        Solution solution,
        Document document,
        Diagnostic diagnostic,
        ConcurrentDictionary<ISymbol, bool> keptTypes,
        CancellationToken cancellationToken)
    {
        var itemChanges = new List<(string File, TextChange Change)>();
        if (!diagnostic.Properties.TryGetValue(CamelCaseNamingAnalyzer.NewNameKey, out var newName) || newName is null)
        {
            return (itemChanges, null, null);
        }

        // Every copy of the file: '#if' code can hold references that only one target framework sees. All or nothing:
        // when one copy's rename isn't safe (another project sees an override or a clash there), the other copies'
        // edits would rename the shared file without it (Polly: an abstract member renamed, the test project's
        // override left behind).
        foreach (var id in document.GetLinkedDocumentIds().Add(document.Id))
        {
            var copy = solution.GetDocument(id);
            if (copy is null
                || await copy.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
                || await copy.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model)
            {
                continue;
            }

            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (token.Span != diagnostic.Location.SourceSpan
                || token.Parent is not { } declaration
                || model.GetDeclaredSymbol(declaration, cancellationToken) is not { } symbol)
            {
                continue;
            }

            var (reason, where) = IsReachableByName(symbol) ? FindInCode(symbol, await GetNamesInCodeAsync(solution).ConfigureAwait(false)) : (null, null);
            if (reason is null)
            {
                (reason, where) = FindQuoted(symbol, await GetNamesInCodeAsync(solution).ConfigureAwait(false), copy.Project, solution, root);
            }

            if (reason is null && symbol is IPropertySymbol { ContainingType: { } owner } && HasKeptProperty(owner, await GetNamesInCodeAsync(solution).ConfigureAwait(false), keptTypes))
            {
                reason = KeptReason.OtherPropertyKept;
            }

            if (reason is null && IsVisibleToOtherProjects(symbol) && (await GetNamesInCodeAsync(solution).ConfigureAwait(false)).Outside.TryGetValue(symbol.Name, out var outside))
            {
                (reason, where) = (KeptReason.UsedInProjectNotLoaded, outside);
            }

            if (reason is not null)
            {
                return (itemChanges, reason, where);
            }

            var (symbolChanges, kept, keptWhere) = await GetChangesAsync(solution, symbol, newName, diagnostic.Id == DiagnosticIds.ParameterMatchesBase, PublicApi.IsRenameAllowed(copy.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree)), cancellationToken).ConfigureAwait(false);
            if (kept is not null)
            {
                return (itemChanges, kept, keptWhere);
            }

            itemChanges.AddRange(symbolChanges);
        }

        // A rename can end a clash that kept IDE0042 quiet ('var r = Get(); int Count;' with '(int Count, ...)').
        if (itemChanges.Count > 0)
        {
            var edits = itemChanges.GroupBy(c => c.File).ToDictionary(g => g.Key, g => g.Select(c => c.Change).ToList());
            if (await Deconstruction.AddsReportsAsync(solution, await ApplyAsync(solution, edits, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false))
            {
                return (new List<(string File, TextChange Change)>(), KeptReason.AddsDeconstruction, null);
            }
        }

        return (itemChanges, null, null);
    }

    /// <summary>
    /// Every string literal in the solution. A field whose name is one of them is left alone: code can reach a private
    /// field by name through reflection, also from other projects the analyzer can't see (Polly's tests read
    /// '_blockedUntil' with GetField), and renaming it would still compile but break at run time. Its diagnostic stays
    /// for a manual rename. Separately, the names in <c>nameof(...)</c> (BRO1409:
    /// <c>GetMethod(nameof(Run))</c> finds public methods only; the naming rules: the rename would change the string, as
    /// with Ocelot's <c>X_RateLimit_Limit = nameof(X_RateLimit_Limit).Replace('_', '-')</c>, an HTTP header name).
    /// </summary>
    private static async Task<NamesInCode> CollectNamesAsync(Solution solution, CancellationToken cancellationToken)
    {
        var strings = new HashSet<string>();
        var reflection = new HashSet<string>();
        var where = new Dictionary<string, string>();
        var nameofs = new HashSet<string>();
        var positional = new Dictionary<string, string>(StringComparer.Ordinal);
        var dynamic = new Dictionary<string, string>(StringComparer.Ordinal);
        var documents = new List<Document>();
        foreach (var project in solution.Projects)
        {
            // Source generators' output too: Mapperly reaches private fields by name ('UnsafeAccessor(..., Name = "intValue")').
            // Not the assembly attributes MSBuild writes under obj/ (company, version): their strings name no member, and as
            // the first string a word was found in they made a misleading reason (trial 27: 'c' kept "first: ...AssemblyInfo.cs").
            documents.AddRange(project.Documents.Where(d => !IsBuildMetadataFile(d.FilePath)));
            documents.AddRange(await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false));
        }

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var document in documents)
        {
            if (document.FilePath is { } path)
            {
                paths.Add(path);
            }

            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is { } root)
            {
                foreach (var (kind, text, line) in RepositoryNames.Collect(root))
                {
                    if (kind == RepositoryNames.NameofKind)
                    {
                        nameofs.Add(text);
                    }
                    else if (kind == RepositoryNames.PositionKind)
                    {
                        AddPositional(text, $"{document.FilePath ?? document.Name}({line + 1})");
                    }
                    else if (kind == RepositoryNames.ReflectionKind)
                    {
                        AddReflection(text, $"{document.FilePath ?? document.Name}({line + 1})");
                    }
                    else if (kind == RepositoryNames.DynamicKind)
                    {
                        AddDynamic(text, $"{document.FilePath ?? document.Name}({line + 1})");
                    }
                    else if (strings.Add(text))
                    {
                        where[text] = $"{document.FilePath ?? document.Name}({line + 1})";
                    }
                }
            }
        }

        // 'bro format' runs one target framework at a time: the files of projects this run doesn't load.
        var outside = new Dictionary<string, string>(StringComparer.Ordinal);
        var outsideMembers = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var entry in ReadRepositoryNames())
        {
            if (entry.Kind == RepositoryNames.NameofKind)
            {
                nameofs.Add(entry.Text);
            }
            else if (entry.Kind == RepositoryNames.PositionKind)
            {
                AddPositional(entry.Text, entry.Where);
            }
            else if (entry.Kind == RepositoryNames.ReflectionKind)
            {
                AddReflection(entry.Text, entry.Where);
            }
            else if (entry.Kind == RepositoryNames.DynamicKind)
            {
                AddDynamic(entry.Text, entry.Where);
            }
            else if (entry.Kind == RepositoryNames.StringKind && strings.Add(entry.Text))
            {
                where[entry.Text] = entry.Where;
            }
            else if (entry.Kind == RepositoryNames.IdentifierKind && !paths.Contains(entry.Where))
            {
                foreach (var name in entry.Text.Split(' '))
                {
                    if (!outside.ContainsKey(name))
                    {
                        outside[name] = entry.Where;
                    }
                }
            }
            else if (entry.Kind == RepositoryNames.MemberNameKind && !paths.Contains(entry.Where))
            {
                foreach (var name in entry.Text.Split(' '))
                {
                    if (!outsideMembers.TryGetValue(name, out var files))
                    {
                        outsideMembers[name] = files = new List<string>();
                    }

                    files.Add(entry.Where);
                }
            }
        }

        return new NamesInCode(strings, nameofs, where, outside, positional, reflection)
        {
            OutsideMembers = outsideMembers,
            Dynamic = dynamic,
            Files = SnapshotFiles.GetWords(solution),
        };

        void AddDynamic(string text, string at)
        {
            if (!dynamic.ContainsKey(text))
            {
                dynamic[text] = at;
            }
        }

        // The reflection read is where the finding points: a field is kept for it, not for the first string with its name.
        void AddReflection(string text, string at)
        {
            if (reflection.Add(text))
            {
                where[text] = at;
            }
        }

        void AddPositional(string text, string at)
        {
            if (!positional.ContainsKey(text))
            {
                positional[text] = at;
            }
        }
    }

    /// <summary>
    /// Whether code can refer to the symbol by a string, and which. A <c>nameof(...)</c> counts only when it refers to the
    /// symbol itself (or a related member renamed with it), which <see cref="GetChangesAsync"/> sees in the references:
    /// matched by name, any <c>nameof(args)</c> of a parameter kept every private field called 'args' (Stateless,
    /// spreadcheetah's 'length').
    /// </summary>
    private static (KeptReason? Reason, string? Where) FindInCode(ISymbol symbol, NamesInCode names)
    {
        // Member access through 'dynamic' binds by name at run time (jint: '((dynamic)t).MemberKey.Field').
        if (names.Dynamic.TryGetValue(symbol.Name, out var dynamicUse))
        {
            return (KeptReason.UsedThroughDynamic, dynamicUse);
        }

        if (MatchInStrings(symbol, names) is { } match)
        {
            return (KeptReason.NameInString, names.WhereIs(match));
        }

        // XAML binds members by name at run time: properties and events (bindings), methods (event handlers), static fields
        // and types ('x:Static', 'x:Class'). Not private fields: XAML can't reach them, and a word in a 'Text="..."' value
        // would keep a common name like 'name'. An element or attribute name is the XAML build's (MC3074, MC3072).
        if (symbol is IFieldSymbol { DeclaredAccessibility: Accessibility.Private })
        {
            return (null, null);
        }

        return names.Files.XamlMarkup.TryGetValue(symbol.Name, out var markup) ? (KeptReason.NameInXamlMarkup, markup)
            : names.Files.Xaml.TryGetValue(symbol.Name, out var xaml) ? (KeptReason.NameInXamlFile, xaml)
            : (null, null);
    }

    /// <summary>
    /// Code a test compares as text (owner's decision 2026-10-20, trial 37: Shouldly's own tests, 37 failures): any rename's old
    /// name in a snapshot or approval file (Shouldly writes the call site's code into its messages, '_target' in
    /// '*.approved.txt'), not only a data member's; and, in a project that uses Shouldly, the old name as a word in a string of
    /// the file ("`methodCall()` should not throw ...").
    /// </summary>
    private static (KeptReason? Reason, string? Where) FindQuoted(ISymbol symbol, NamesInCode names, Project project, Solution solution, SyntaxNode root)
    {
        if (names.Files.Snapshots.TryGetValue(symbol.Name, out var snapshot))
        {
            return (KeptReason.NameInSnapshotFile, snapshot);
        }

        if (names.Files.ApiListings.TryGetValue(symbol.Name, out var listing) && IsInPublicApi(symbol))
        {
            return (KeptReason.NameInSnapshotFile, listing);
        }

        if (!UsesShouldly(project, solution))
        {
            return (null, null);
        }

        foreach (var token in TreeWalk.Tokens(root))
        {
            if ((token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken) || token.IsKind(SyntaxKind.Utf8StringLiteralToken)
                || token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) || token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
                && ContainsWord(token.ValueText, symbol.Name))
            {
                return (KeptReason.NameInShouldlyMessage, $"{root.SyntaxTree.FilePath}({token.GetLocation().GetLineSpan().StartLinePosition.Line + 1})");
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Whether a public-API listing can name the symbol: it and every type around it are public or protected (a parameter
    /// or type parameter by its member). Private and internal members, and locals, are never in one.
    /// </summary>
    internal static bool IsInPublicApi(ISymbol symbol)
    {
        var current = symbol is IParameterSymbol or ITypeParameterSymbol ? symbol.ContainingSymbol : symbol;
        for (; current is not null and not INamespaceSymbol; current = current.ContainingSymbol)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
            {
                return false;
            }
        }

        return current is not null;
    }

    /// <summary>Whether the project references Shouldly: the package's assembly, or the Shouldly project itself (its own tests).</summary>
    private static bool UsesShouldly(Project project, Solution solution) =>
        project.MetadataReferences.OfType<PortableExecutableReference>().Any(r => string.Equals(System.IO.Path.GetFileNameWithoutExtension(r.FilePath), "Shouldly", StringComparison.OrdinalIgnoreCase))
        || project.ProjectReferences.Any(r => string.Equals(solution.GetProject(r.ProjectId)?.AssemblyName, "Shouldly", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether <paramref name="text"/> has <paramref name="word"/> with no letter, digit or '_' right before or after it.</summary>
    internal static bool ContainsWord(string text, string word)
    {
        for (var at = text.IndexOf(word, StringComparison.Ordinal); at >= 0; at = text.IndexOf(word, at + 1, StringComparison.Ordinal))
        {
            var end = at + word.Length;
            if ((at == 0 || !IsWordChar(text[at - 1])) && (end == text.Length || !IsWordChar(text[end])))
            {
                return true;
            }
        }

        return false;

        static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
    }

    /// <summary>
    /// Names that serializers and script engines write and read: instance properties and events, public and internal
    /// instance fields (whatever the containing type's accessibility: reflection doesn't care, jint's scripts read
    /// 'host.ByteField' of a private class), and enum members.
    /// </summary>
    private static bool IsDataSymbol(ISymbol symbol) =>
        (symbol is IPropertySymbol or IEventSymbol && !symbol.IsStatic)
        || (symbol is IFieldSymbol field && (FieldNames.IsDataMember(field) || field.ContainingType.TypeKind == TypeKind.Enum));

    /// <summary>
    /// The first string code can refer to the symbol by, or null: a field by its name (GetField("_count")), a type by its name
    /// or its qualified name (Type.GetType("App.Shape"), "App.Shape, App"), a nested type also after '+' ("App.Tests+Shape is ...").
    /// </summary>
    private static string? MatchInStrings(ISymbol symbol, NamesInCode names)
    {
        var strings = names.Strings;

        // Names that serializers write (public instance fields, instance properties and events, enum members) are looked
        // for inside strings too, like test JSON. Private fields only by their exact name: other projects reach them only by
        // reflection ('GetField("_count")'), and a common word like 'count' in any message would block every rename.
        // (Inside the field's own type, the analyzer also matches it inside strings: 'DebuggerDisplay("{_count}")'.)
        var isData = IsDataSymbol(symbol);

        // Other fields (private ones, mostly) and static properties and events (no serializer writes them) only by a string
        // where code names a member to reach it ('GetField("_count")', RepositoryNames.IsReflectionShaped; owner's decision,
        // docs/decisions.md): any string at all kept 692 of Orleans' renames for words like "options" in
        // 'Assert.Equal("options", e.ParamName)', and Flow.Launcher's 'type' for a JSON key in another plugin. AngleSharp's
        // test assets ('static string nbc => Read("Pages.nbc.html")') were kept for URLs and encoding names (trial 21).
        // A name only a field would have ('_root', 'm_count': a leading underscore or prefix) counts in any string (owner's
        // decision 2026-10-17, trial 29: Harmony's tests list '"_root", "_type"' and read each with
        // AccessTools.DeclaredField in a loop); no message says '_root', so the common words stay renamed.
        if (symbol is IFieldSymbol or IPropertySymbol or IEventSymbol && !isData)
        {
            return names.Reflection.Contains(symbol.Name) || (symbol is IFieldSymbol && LooksLikeFieldName(symbol.Name) && strings.Contains(symbol.Name))
                ? symbol.Name
                : null;
        }

        if (symbol is not INamedTypeSymbol)
        {
            var word = new System.Text.RegularExpressions.Regex(@"(?<![\w@])" + System.Text.RegularExpressions.Regex.Escape(symbol.Name) + @"(?!\w)");
            return strings.Contains(symbol.Name) ? symbol.Name : isData ? strings.FirstOrDefault(s => s.IndexOf(symbol.Name, StringComparison.Ordinal) >= 0 && word.IsMatch(s)) : null;
        }

        // A nested type as reflection writes it ('Tests+PrivateInterface') counts with anything after it: Castle Core's tests
        // compare 'Can not create proxy for type ...TestCase+PrivateInterface because ...' (trial 35).
        var escaped = System.Text.RegularExpressions.Regex.Escape(symbol.Name);
        var pattern = new System.Text.RegularExpressions.Regex(@"(^|\.)" + escaped + @"($|[,`\[+])|\+" + escaped + @"(?!\w)");
        return strings.FirstOrDefault(s => pattern.IsMatch(s));
    }

    // '_x' or a one-letter prefix ('m_x', 's_x', 't_x'): no word in a message looks like that.
    private static bool LooksLikeFieldName(string name) =>
        name.Length > 1 && (name[0] == '_' || (name.Length > 2 && name[1] == '_' && name[0] is 'm' or 's' or 't'));

    /// <summary>
    /// Whether a property of the type that BRO1309 would rename keeps its name because it's in a string (test JSON, a wire
    /// format). Then the type's other properties keep theirs too: renaming only some mixes casings and changes only part
    /// of what a serializer reads and writes (Kavita's Koreader DTO: 'percentage' renamed, 'document' and 'progress' kept).
    /// A snake_case property ('line_count') is never BRO1309's, so it keeps its name too, and so do the others (trial 23).
    /// </summary>
    private static bool HasKeptProperty(INamedTypeSymbol type, NamesInCode names, ConcurrentDictionary<ISymbol, bool> cache) =>
        cache.GetOrAdd(type, _ => type.GetMembers().OfType<IPropertySymbol>()
            .Any(p => p.Name.TrimStart('_').IndexOf('_') >= 0
                || (PascalCaseNamingAnalyzer.GetNewName(p.Name) is not null && FindInCode(p, names).Reason is not null)));

    /// <summary>Symbols whose conflicts are checked by syntax over their member (<see cref="CamelCaseNames.CanRename"/>).</summary>
    private static bool IsMemberScoped(ISymbol symbol) =>
        symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol or IMethodSymbol { MethodKind: MethodKind.LocalFunction };

    /// <summary>Symbols that code can reach by a name in a string (reflection, serializers, type names).</summary>
    private static bool IsReachableByName(ISymbol symbol) =>
        symbol is IFieldSymbol or INamedTypeSymbol or IPropertySymbol or IEventSymbol
            || symbol is IMethodSymbol { MethodKind: not MethodKind.LocalFunction };

    /// <summary>Type members whose rename has to follow overrides and implementations.</summary>
    private static bool IsTypeMember(ISymbol symbol) =>
        symbol is IPropertySymbol or IEventSymbol || symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary };

    /// <summary>
    /// A static field that registers a XAML framework's property (<c>LevelProperty</c>, an Avalonia, WPF/WinUI or MAUI property
    /// type, also a read-only one's <c>LevelPropertyKey</c>) or routed event (<c>ClickEvent</c>): XAML finds the registration by
    /// that name (attached properties, dependency property lookups), and Avalonia's AVP1022 reports any other name (trial 31:
    /// Ursa.Avalonia's internal <c>LevelProperty</c> renamed to the repository's '_levelProperty').
    /// </summary>
    internal static bool IsRegistrationField(IFieldSymbol field)
    {
        if (!field.IsStatic)
        {
            return false;
        }

        var names = field.Name.EndsWith("Property", StringComparison.Ordinal) ? new[] { "AvaloniaProperty", "DependencyProperty", "BindableProperty" }
            : field.Name.EndsWith("PropertyKey", StringComparison.Ordinal) ? new[] { "DependencyPropertyKey", "BindablePropertyKey" }
            : field.Name.EndsWith("Event", StringComparison.Ordinal) ? new[] { "RoutedEvent" }
            : null;
        for (var type = field.Type as INamedTypeSymbol; names is not null && type is not null; type = type.BaseType)
        {
            if (Array.IndexOf(names, type.Name) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>'IShape.area' (an explicit implementation's name) -> 'area'.</summary>
    private static string SimpleName(string name) => name.Substring(name.LastIndexOf('.') + 1);

    /// <summary>
    /// The edits that rename <paramref name="symbol"/> (and, for a parameter, the same-named parameters of its
    /// overrides and implementations), or why not when one of them isn't safe to rename. Without
    /// <paramref name="renamePublicApi"/>, a related member other assemblies see (a public class's implementation of an
    /// internal interface's method) makes the rename unsafe, and a related parameter they see keeps its name.
    /// </summary>
    private static async Task<(List<(string File, TextChange Change)> Changes, KeptReason? Reason, string? Where)> GetChangesAsync(
        Solution solution,
        ISymbol symbol,
        string newName,
        bool keepObservableNames,
        bool renamePublicApi,
        CancellationToken cancellationToken)
    {
        var oldName = symbol.Name;
        var result = new List<(string File, TextChange Change)>();
        var symbols = new List<ISymbol> { symbol };
        if (symbol is IParameterSymbol parameter)
        {
            await AddRelatedAsync(solution, parameter, parameter.ContainingSymbol, parameter.Ordinal, GetParameters, symbols, cancellationToken).ConfigureAwait(false);
        }
        else if (symbol is ITypeParameterSymbol { DeclaringMethod: { } method } typeParameter)
        {
            await AddRelatedAsync(solution, typeParameter, method, typeParameter.Ordinal, GetTypeParameters, symbols, cancellationToken).ConfigureAwait(false);
        }
        else if (IsTypeMember(symbol))
        {
            await AddRelatedMembersAsync(solution, symbol, symbols, cancellationToken).ConfigureAwait(false);

            // An implementation that also implements or overrides a member that isn't renamed (another interface's
            // 'Run', a library's) would stop doing so. Compared by name, not symbol: another project's view of a
            // multi-targeted library is a different (retargeted) symbol for the same member.
            var renamed = new HashSet<string>(symbols.Select(s => s.OriginalDefinition.ToDisplayString()));
            if (!renamePublicApi && symbols.Any(PublicApi.IsVisible))
            {
                return (result, KeptReason.PublicApi, null);
            }

            if (symbols.Any(s => CamelCaseNamingAnalyzer.GetBaseMembers(s).Any(b => !renamed.Contains(b.OriginalDefinition.ToDisplayString()))))
            {
                return (result, KeptReason.ImplementsAnotherMember, null);
            }

            if (await HasDerivedMemberAsync(solution, symbols, newName, cancellationToken).ConfigureAwait(false))
            {
                return (result, KeptReason.DerivedMemberHasNewName, null);
            }
        }
        else if (symbol is INamedTypeSymbol type)
        {
            // Constructors and the finalizer carry the type's name.
            symbols.AddRange(type.InstanceConstructors.Concat(type.StaticConstructors)
                .Concat(type.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Destructor))
                .Where(m => !m.IsImplicitlyDeclared));
        }

        if (symbol is IFieldSymbol field && IsRegistrationField(field))
        {
            return (result, KeptReason.RegistrationField, null);
        }

        if ((IsTypeMember(symbol) || symbol is IFieldSymbol || symbol is INamedTypeSymbol { ContainingType: not null }) && await IsHiddenByNewMemberAsync(solution, symbol, cancellationToken).ConfigureAwait(false))
        {
            return (result, KeptReason.HiddenByNewMember, null);
        }

        foreach (var current in symbols)
        {
            // A related parameter that can't be renamed safely keeps its name (it's then no longer reported either,
            // since the same conflict blocks its own rename); the reported symbol itself was checked by the analyzer.
            if (!SymbolEqualityComparer.Default.Equals(current, symbol)
                && IsMemberScoped(current)
                && (!await CanRenameAsync(current).ConfigureAwait(false)
                    || (!renamePublicApi && PublicApi.IsVisible(current))
                    || (keepObservableNames && current is IParameterSymbol && await IsObservableAsync(current).ConfigureAwait(false))))
            {
                continue;
            }

            foreach (var location in current.Locations.Where(l => l.IsInSource))
            {
                var tree = location.SourceTree!;
                var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var declaration = root.FindToken(location.SourceSpan.Start).Parent;

                // Fields, types and type parameters were checked by their analyzers; references are checked below.
                if (declaration is null || (IsMemberScoped(current) && !CamelCaseNames.CanRename(declaration, oldName, newName)))
                {
                    return (result, KeptReason.NewNameTaken, null);
                }

                if (await TryAddAsync(solution.GetDocument(tree), location.SourceSpan, newName).ConfigureAwait(false) is { } declarationKept)
                {
                    return (result, declarationKept, null);
                }

                if (current is ILocalSymbol or IParameterSymbol && solution.GetDocument(tree) is { } scopeDocument)
                {
                    result.AddRange(await GetUnneededMemberThisAsync(scopeDocument, declaration, location.SourceSpan, current, oldName, cancellationToken).ConfigureAwait(false));
                }
            }

            foreach (var referenced in await SymbolFinder.FindReferencesAsync(current, solution, cancellationToken).ConfigureAwait(false))
            {
                // A constructor's references are 'new T(...)' (renamed with the type) and 'this(...)'/'base(...)' (left).
                var isConstructor = referenced.Definition is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor };
                if (SimpleName(referenced.Definition.Name) != oldName && !isConstructor)
                {
                    continue;
                }

                foreach (var reference in referenced.Locations)
                {
                    // Owner's choice 2026-10-15, "Leave VB alone": StyleBro doesn't edit Visual Basic code (case-insensitive:
                    // its lookups found the old name under the new one), so a name VB uses keeps it.
                    if (reference.Document.Project.Language != LanguageNames.CSharp)
                    {
                        return (result, KeptReason.UsedFromVisualBasic, null);
                    }

                    if (reference.IsImplicit
                        || (isConstructor
                            && (await reference.Document.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString(reference.Location.SourceSpan).TrimStart('@') != oldName))
                    {
                        continue;
                    }

                    // nameof turns the name into a run-time string (an HTTP header name, 'GetField(nameof(_tracer))').
                    if (IsReachableByName(symbol)
                        && (await reference.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))?.FindToken(reference.Location.SourceSpan.Start) is { } token
                        && CamelCaseNames.IsNameofName(token))
                    {
                        return (result, KeptReason.NameInNameof, null);
                    }

                    if (await GetLogKeyHandlerAsync(reference.Document, reference.Location.SourceSpan, cancellationToken).ConfigureAwait(false) is { } handler)
                    {
                        var line = (await reference.Document.GetTextAsync(cancellationToken).ConfigureAwait(false)).Lines.GetLineFromPosition(reference.Location.SourceSpan.Start).LineNumber;
                        return (result, KeptReason.NameIsLogKey, $"{reference.Document.FilePath ?? reference.Document.Name}({line + 1}) in {handler}");
                    }

                    var replacement = await GetReplacementAsync(current, reference.Document, reference.Location.SourceSpan, newName, cancellationToken).ConfigureAwait(false);
                    if (replacement is null)
                    {
                        return (result, KeptReason.NewNameMeansSomethingElse, null);
                    }

                    var editSpan = await GetUnneededThisAsync(current, reference.Document, reference.Location.SourceSpan, oldName, newName, cancellationToken).ConfigureAwait(false);
                    if (await TryAddAsync(reference.Document, reference.Location.SourceSpan, replacement, editSpan).ConfigureAwait(false) is { } referenceKept)
                    {
                        return (result, referenceKept, null);
                    }
                }
            }
        }

        if (symbol is ITypeParameterSymbol renamedTypeParameter)
        {
            foreach (var (document, span) in await GetCrefTypeParametersAsync(solution, renamedTypeParameter, newName, cancellationToken).ConfigureAwait(false))
            {
                var key = GetFileKey(document);
                if (!result.Any(r => r.File == key && r.Change.Span == span))
                {
                    result.Add((key, new TextChange(span, newName)));
                }
            }
        }

        return (result, null, null);

        async Task<bool> CanRenameAsync(ISymbol related)
        {
            foreach (var location in related.Locations.Where(l => l.IsInSource))
            {
                var root = await location.SourceTree!.GetRootAsync(cancellationToken).ConfigureAwait(false);
                if (root.FindToken(location.SourceSpan.Start).Parent is not { } declaration
                    || !CamelCaseNames.CanRename(declaration, oldName, newName))
                {
                    return false;
                }
            }

            return true;
        }

        // BRO1313: an override's parameter renamed along with its base's keeps a name that reaches run time (nameof).
        async Task<bool> IsObservableAsync(ISymbol related)
        {
            foreach (var location in related.Locations.Where(l => l.IsInSource))
            {
                if (solution.GetDocument(location.SourceTree) is { } document
                    && await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is { } model
                    && (await location.SourceTree!.GetRootAsync(cancellationToken).ConfigureAwait(false)).FindToken(location.SourceSpan.Start).Parent?.Parent?.Parent is { } member
                    && CamelCaseNames.IsNameObservable(member, oldName, model, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        // Null when the edit is added, else why it can't be.
        async Task<KeptReason?> TryAddAsync(Document? document, TextSpan span, string replacement, TextSpan? editSpan = null)
        {
            // Generated code can't be renamed with the rest (a tool writes it again): a Razor page's or a source generator's
            // reference would be left behind.
            if (document is null or SourceGeneratedDocument
                || await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false) is not { } tree
                || NamespaceNames.IsGenerated(tree))
            {
                return KeptReason.GeneratedReference;
            }

            // Only ever replace the old name itself ('@Name' included).
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text.ToString(span).TrimStart('@') != oldName)
            {
                return KeptReason.ReferenceNotByName;
            }

            result.Add((GetFileKey(document), new TextChange(editSpan ?? span, replacement)));
            return null;
        }
    }

    /// <summary>
    /// The text that replaces a reference: the new name, or for a field whose new name a local, parameter or other
    /// symbol would hide at that spot, the qualified name ('this.count', or 'Type.count' for a static field). Null for
    /// a type or type parameter whose new name already means something at that spot (the whole rename is skipped).
    /// </summary>
    private static async Task<string?> GetReplacementAsync(
        ISymbol symbol,
        Document document,
        TextSpan span,
        string newName,
        CancellationToken cancellationToken)
    {
        if (symbol is INamedTypeSymbol or ITypeParameterSymbol)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return semanticModel is null || !semanticModel.LookupSymbols(span.Start, name: newName).IsEmpty ? null : newName;
        }

        if (symbol is IMethodSymbol or IPropertySymbol or IEventSymbol)
        {
            // A simple name ('area()', not 'x.area()') is looked up in scope, where the new name may mean something else.
            var memberRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (memberRoot?.FindToken(span.Start).Parent is IdentifierNameSyntax simple && IsSimpleName(simple)
                && await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is { } memberModel
                && !memberModel.LookupSymbols(span.Start, name: newName).IsEmpty)
            {
                return null;
            }

            return newName;
        }

        if (symbol is not IFieldSymbol field
            || await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
            || root.FindToken(span.Start).Parent is not IdentifierNameSyntax name
            || !IsSimpleName(name)
            || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model
            || model.LookupSymbols(span.Start, name: newName)
                .Where(s => !IsHiddenByTheRename(s, field, newName) && !SymbolEqualityComparer.Default.Equals(s, field.Type))
                .ToList() is not { Count: > 0 } found)
        {
            return newName;
        }

        // The field's own type by the new name ('SimulatorEvent _simulatorEvent' -> 'SimulatorEvent SimulatorEvent') is
        // legal ('Color Color': 'SimulatorEvent.Create()' still binds to the type), and members of enclosing types are
        // hidden by the renamed field (both filtered above; trial 21).
        // Only locals and parameters can be stepped around by qualifying; another member with the new name (for
        // example in a derived type, which would hide the field) means the rename is skipped.
        if (found.Any(s => s is not (ILocalSymbol or IParameterSymbol or IRangeVariableSymbol)))
        {
            return null;
        }

        var qualifier = field.IsStatic ? field.ContainingType.ToMinimalDisplayString(model, span.Start) : "this";
        return qualifier + "." + newName;
    }

    /// <summary>
    /// The span of 'this.oldName' when the rename makes the 'this.' redundant (Fluid: 'this.pet = pet' would become
    /// 'this._pet = pet'), so the whole access is replaced by the new name: only a 'this.' the original code needed (a
    /// local or parameter named like the field), when nothing at that spot is named like the new name (the plain new
    /// name then binds to the field) and the repo doesn't ask for 'this.' (dotnet_style_qualification_for_field true).
    /// Null to keep the qualifier.
    /// </summary>
    private static async Task<TextSpan?> GetUnneededThisAsync(ISymbol symbol, Document document, TextSpan span, string oldName, string newName, CancellationToken cancellationToken)
    {
        if (symbol is not IFieldSymbol { IsStatic: false }
            || await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
            || root.FindToken(span.Start).Parent is not IdentifierNameSyntax { Parent: MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access } name
            || access.Name != name
            || !access.Expression.GetTrailingTrivia().Concat(access.OperatorToken.LeadingTrivia).Concat(access.OperatorToken.TrailingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
            || (document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree).TryGetValue("dotnet_style_qualification_for_field", out var value)
                && value.Split(':')[0].Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model
            || !model.LookupSymbols(access.SpanStart, name: newName).IsEmpty
            || !model.LookupSymbols(access.SpanStart, name: oldName).Any(s => s is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol))
        {
            return null;
        }

        return access.Span;
    }

    /// <summary>
    /// The old name in the doc comment crefs of the type parameter's own type or method that spell its type parameters
    /// (<c>&lt;see cref="Node{_TVertex}"/&gt;</c>): a cref declares type parameters of its own there, so references don't
    /// find them, and BRO1606 ("Initializes a new instance of the <see cref="Node{TVertex}"/> class") fixed them a run later
    /// (trial 28). Every use of the old name in such a cref is renamed with it; a cref that already uses the new name is left.
    /// </summary>
    private static async Task<List<(Document Document, TextSpan Span)>> GetCrefTypeParametersAsync(Solution solution, ITypeParameterSymbol typeParameter, string newName, CancellationToken cancellationToken)
    {
        var found = new List<(Document Document, TextSpan Span)>();
        ISymbol owner = (ISymbol?)typeParameter.DeclaringMethod ?? typeParameter.DeclaringType!;
        var arity = owner is IMethodSymbol method ? method.Arity : ((INamedTypeSymbol)owner).Arity;
        foreach (var reference in (owner.ContainingType ?? owner).DeclaringSyntaxReferences)
        {
            if (solution.GetDocument(reference.SyntaxTree) is not { } document)
            {
                continue;
            }

            var crefs = (await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false))
                .DescendantNodes(descendIntoTrivia: true)
                .OfType<XmlCrefAttributeSyntax>()
                .Select(a => a.Cref)
                .Where(cref => cref.DescendantNodes().OfType<GenericNameSyntax>().Any(g =>
                    g.Identifier.ValueText == owner.Name
                    && g.TypeArgumentList.Arguments.Count == arity
                    && g.TypeArgumentList.Arguments[typeParameter.Ordinal] is IdentifierNameSyntax { Identifier.ValueText: var name }
                    && name == typeParameter.Name));
            foreach (var cref in crefs)
            {
                var names = cref.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();
                if (names.All(n => n.Identifier.ValueText != newName))
                {
                    found.AddRange(names.Where(n => n.Identifier.ValueText == typeParameter.Name).Select(n => (document, n.Identifier.Span)));
                }
            }
        }

        return found;
    }

    /// <summary>
    /// The edits that drop a 'this.' that only the renamed local or parameter made necessary ('var Download = ...;' next to
    /// 'this.Download(...)'), where the repository asks for no qualifier (dotnet_style_qualification_for_* false, like
    /// BRO1131's fix): BRO1131 writes 'this.' there while the local shadows the method, and without this the result
    /// depended on whether BRO1131 or BRO1301 ran first (trial 28, Jackett). Only where the old name means the renamed
    /// symbol and nothing else in its scope declares it, so the plain name then binds the member; not for extension methods.
    /// </summary>
    private static async Task<List<(string File, TextChange Change)>> GetUnneededMemberThisAsync(Document document, SyntaxNode declaration, TextSpan declared, ISymbol renamed, string oldName, CancellationToken cancellationToken)
    {
        var edits = new List<(string File, TextChange Change)>();
        var scope = CamelCaseNames.GetScope(declaration);
        var accesses = scope.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(a => a.Expression is ThisExpressionSyntax && a.Name.Identifier.ValueText == oldName)
            .ToList();
        if (accesses.Count == 0
            || scope.DescendantTokens().Any(t => t.ValueText == oldName && t.Span != declared && IsDeclaration(t))
            || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model)
        {
            return edits;
        }

        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(declaration.SyntaxTree);
        foreach (var access in accesses)
        {
            ExpressionSyntax target = access.Parent is InvocationExpressionSyntax invocation && invocation.Expression == access ? invocation : access;
            if (access.Expression.GetTrailingTrivia().Concat(access.OperatorToken.LeadingTrivia).Concat(access.OperatorToken.TrailingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                && model.GetSymbolInfo(target, cancellationToken).Symbol is { } member
                && member is not IMethodSymbol { ReducedFrom: not null }
                && Analyzers.Readability.BaseCalls.PrefersNoQualifier(member, options)
                && model.LookupSymbols(access.SpanStart, name: oldName) is { Length: > 0 } found
                && found.All(s => SymbolEqualityComparer.Default.Equals(s, renamed)))
            {
                edits.Add((GetFileKey(document), new TextChange(TextSpan.FromBounds(access.SpanStart, access.Name.SpanStart), string.Empty)));
            }
        }

        return edits;

        static bool IsDeclaration(SyntaxToken token) => token.Parent is VariableDeclaratorSyntax or ParameterSyntax or SingleVariableDesignationSyntax
            or ForEachStatementSyntax or CatchDeclarationSyntax or LocalFunctionStatementSyntax or TypeParameterSyntax
            or FromClauseSyntax or LetClauseSyntax or JoinClauseSyntax or JoinIntoClauseSyntax or QueryContinuationSyntax;
    }

    /// <summary>
    /// Whether <paramref name="found"/> is a member of a type enclosing the field's type (or of its bases) that the field's
    /// type never names: the renamed field hides it at the field's uses, since lookup searches the type and its bases before
    /// the enclosing types, and no other use in the type means it (AngleSharp, trial 21: a nested state class's 'valid' was
    /// kept for the outer class's '_valid').
    /// </summary>
    private static bool IsHiddenByTheRename(ISymbol found, IFieldSymbol field, string newName)
    {
        var type = field.ContainingType;
        for (var outer = type.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            for (var t = outer; t is not null; t = t.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(found.ContainingType?.OriginalDefinition, t.OriginalDefinition))
                {
                    return !type.DeclaringSyntaxReferences.Any(r => r.GetSyntax().DescendantTokens().Any(token => token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == newName));
                }
            }
        }

        return false;
    }

    /// <summary>A name that is looked up in scope, as opposed to the member part of 'x.Name' or 'Name = ...'.</summary>
    private static bool IsSimpleName(IdentifierNameSyntax name)
    {
        return name.Parent switch
        {
            MemberAccessExpressionSyntax access when access.Name == name => false,
            MemberBindingExpressionSyntax or NameColonSyntax or NameEqualsSyntax or XmlNameAttributeSyntax => false,
            QualifiedNameSyntax qualified when qualified.Right == name => false,
            AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment when assignment.Left == name => false,
            _ => true,
        };
    }

    /// <summary>
    /// Adds the overrides and implementations (explicit ones included) of <paramref name="member"/> that have the same
    /// name, recursively: renaming an abstract, virtual or interface member without them wouldn't compile.
    /// </summary>
    private static async Task AddRelatedMembersAsync(Solution solution, ISymbol member, List<ISymbol> symbols, CancellationToken cancellationToken)
    {
        var related = new List<ISymbol>();
        related.AddRange(await SymbolFinder.FindOverridesAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        if (member.ContainingType?.TypeKind == TypeKind.Interface)
        {
            related.AddRange(await SymbolFinder.FindImplementationsAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        foreach (var relatedMember in related)
        {
            if (SimpleName(relatedMember.Name) == SimpleName(member.Name)
                && relatedMember.Locations.Any(l => l.IsInSource)
                && !symbols.Contains(relatedMember, SymbolEqualityComparer.Default))
            {
                symbols.Add(relatedMember);
                await AddRelatedMembersAsync(solution, relatedMember, symbols, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Whether a type deriving from (or implementing) one of the renamed members' types already has a member with the
    /// new name: the renamed member would then clash with it or be hidden by it.
    /// </summary>
    private static async Task<bool> HasDerivedMemberAsync(Solution solution, List<ISymbol> members, string newName, CancellationToken cancellationToken)
    {
        foreach (var type in members.Select(m => m.ContainingType).Where(t => t is not null).Distinct(SymbolEqualityComparer.Default).Cast<INamedTypeSymbol>())
        {
            var derived = type.TypeKind == TypeKind.Interface
                ? (await SymbolFinder.FindImplementationsAsync(type, solution, cancellationToken: cancellationToken).ConfigureAwait(false)).OfType<INamedTypeSymbol>()
                : await SymbolFinder.FindDerivedClassesAsync(type, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (derived.Any(d => !d.GetMembers(newName).IsEmpty))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a derived type hides the member with a 'new' member of its name (<see cref="PascalCaseNamingAnalyzer.HidesBaseMember"/>):
    /// renamed, the member would no longer be hidden, and that 'new' would be CS0109. A private member isn't hidden.
    /// </summary>
    private static async Task<bool> IsHiddenByNewMemberAsync(Solution solution, ISymbol member, CancellationToken cancellationToken)
    {
        if (member.DeclaredAccessibility == Accessibility.Private || member.ContainingType is not { IsSealed: false } type)
        {
            return false;
        }

        var derived = type.TypeKind == TypeKind.Interface
            ? await SymbolFinder.FindDerivedInterfacesAsync(type, solution, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await SymbolFinder.FindDerivedClassesAsync(type, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
        return derived.Any(d => d.GetMembers(member.Name).Any(PascalCaseNamingAnalyzer.HidesBaseMember));
    }

    /// <summary>
    /// Adds the parameters (or type parameters) at the same position in the overrides and implementations of
    /// <paramref name="member"/> that have the same name, recursively, so they are renamed together.
    /// </summary>
    private static async Task AddRelatedAsync(
        Solution solution,
        ISymbol item,
        ISymbol member,
        int ordinal,
        Func<ISymbol, ImmutableArray<ISymbol>> getItems,
        List<ISymbol> symbols,
        CancellationToken cancellationToken)
    {
        var related = new List<ISymbol>();
        related.AddRange(await SymbolFinder.FindOverridesAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        if (member.ContainingType?.TypeKind == TypeKind.Interface)
        {
            related.AddRange(await SymbolFinder.FindImplementationsAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        foreach (var relatedMember in related)
        {
            var items = getItems(relatedMember);
            if (ordinal < items.Length
                && items[ordinal] is { } relatedItem
                && relatedItem.Name == item.Name
                && relatedItem.Locations.Any(l => l.IsInSource)
                && !symbols.Contains(relatedItem, SymbolEqualityComparer.Default))
            {
                symbols.Add(relatedItem);
                await AddRelatedAsync(solution, relatedItem, relatedMember, ordinal, getItems, symbols, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static ImmutableArray<ISymbol> GetParameters(ISymbol member) => member switch
    {
        IMethodSymbol method => method.Parameters.CastArray<ISymbol>(),
        IPropertySymbol property => property.Parameters.CastArray<ISymbol>(),
        _ => ImmutableArray<ISymbol>.Empty,
    };

    private static ImmutableArray<ISymbol> GetTypeParameters(ISymbol member) =>
        member is IMethodSymbol method ? method.TypeParameters.CastArray<ISymbol>() : ImmutableArray<ISymbol>.Empty;

    /// <summary>The string literals (and interpolated text) and the names in <c>nameof(...)</c> of a solution.</summary>
    internal sealed class NamesInCode
    {
        private readonly Dictionary<string, string> where;

        public NamesInCode(HashSet<string> strings, HashSet<string> nameofs, Dictionary<string, string> where, Dictionary<string, string>? outside = null, Dictionary<string, string>? positional = null, HashSet<string>? reflection = null)
        {
            Reflection = reflection ?? new HashSet<string>();
            Positional = positional ?? new Dictionary<string, string>(StringComparer.Ordinal);
            Strings = strings;
            Nameofs = nameofs;
            this.where = where;
            Outside = outside ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public HashSet<string> Strings { get; }

        public HashSet<string> Nameofs { get; }

        /// <summary>Gets the strings where code names a member to reach it (<see cref="RepositoryNames.IsReflectionShaped"/>).</summary>
        public HashSet<string> Reflection { get; }

        /// <summary>
        /// Types whose members code reads by position ('Name GetFields', <see cref="StyleBro.Analyzers.Ordering.ReflectionByPosition"/>),
        /// with where the first such read is ('path(line)').
        /// </summary>
        public Dictionary<string, string> Positional { get; }

        /// <summary>Identifiers in files of projects this run didn't load, with the first such file (<see cref="RepositoryNames"/>).</summary>
        public Dictionary<string, string> Outside { get; }

        /// <summary>Names used as members (<see cref="RepositoryNames.MemberNameKind"/>) in files of projects this run didn't load, with those files.</summary>
        public Dictionary<string, List<string>> OutsideMembers { get; set; } = new(StringComparer.Ordinal);

        /// <summary>Gets or sets the names used as members through <see langword="dynamic"/> (<see cref="RepositoryNames.DynamicKind"/>), with where.</summary>
        public Dictionary<string, string> Dynamic { get; set; } = new(StringComparer.Ordinal);

        /// <summary>Gets or sets the words of the snapshot, approval and XAML files (<see cref="SnapshotFiles"/>), with where.</summary>
        public SnapshotFiles.Words Files { get; set; } = new();

        public bool Contains(string name) => Strings.Contains(name) || Nameofs.Contains(name);

        /// <summary>Where the string first appears ('path(line)'), or null.</summary>
        public string? WhereIs(string text) => where.TryGetValue(text, out var at) ? at : null;
    }
}
