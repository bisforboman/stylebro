using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;

namespace StyleBro.Host;

/// <summary>
/// The order the host fixes ids in, and which fixers it calls (owner's decision 2026-10-17: a fixed order). dotnet format
/// takes the ids in a hash set's order, which changes from one process to the next; the host takes them in ordinal order
/// (BRO1001 ... BRO1619, then CA..., IDE..., xUnit...), the code style formatter's before the analyzer references', as
/// dotnet format runs those two. Any fixed order would do: <c>FixOrderTests</c> requires the same text in every order for the
/// ids whose fixes meet, and an order that gives other text is a bug of those fixes, found by the parity runs, not something
/// the order should hide. Ordinal is the one that needs no list to keep up to date.
/// </summary>
public static class FixOrder
{
    /// <summary>The ids in the order the host fixes them.</summary>
    public static List<string> Of(IEnumerable<string> ids) => [.. ids.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>
    /// The fixers for <paramref name="id"/>, each type once. dotnet format collects every project's fixers and calls each one's
    /// Fix All (17 calls per id on Newtonsoft.Json, docs/proposals/fix-loop-costs-2026-10.md): only the first changes anything,
    /// since the others get the diagnostics of the documents the first one replaced. IDE0005's fixer reports
    /// 'RemoveUnnecessaryImportsFixable', as dotnet format matches it. With <paramref name="languages"/> (the languages of the
    /// projects the diagnostics are in), only fixers exported for one of them (or for no language in particular): a C# project
    /// next to a Visual Basic one got the VB fixer of IDE0036 after the C# one, and it deleted the C# method whose modifiers it
    /// was handed (fixture matrix, visual-basic).
    /// </summary>
    public static List<CodeFixProvider> FixersFor(string id, IEnumerable<CodeFixProvider> fixers, IReadOnlyCollection<string>? languages = null) =>
        [.. fixers.Where(f => f.FixableDiagnosticIds.Contains(id) || (id == "IDE0005" && f.FixableDiagnosticIds.Contains("RemoveUnnecessaryImportsFixable")))
            .Where(f => languages is null || LanguagesOf(f) is not { Count: > 0 } own || own.Overlaps(languages))
            .GroupBy(f => f.GetType())
            .Select(g => g.First())];

    /// <summary>The languages <paramref name="fixer"/> is exported for (its ExportCodeFixProvider attributes), empty without one.</summary>
    public static HashSet<string> LanguagesOf(CodeFixProvider fixer) =>
        new(fixer.GetType().GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), inherit: false).OfType<ExportCodeFixProviderAttribute>().SelectMany(a => a.Languages), StringComparer.Ordinal);
}
