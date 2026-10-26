using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace StyleBro.CodeFixes;

/// <summary>
/// <see cref="SymbolFinder.FindReferencesAsync(ISymbol, Solution, CancellationToken)"/>, made again when Roslyn's own race
/// hits it. Roslyn 4.8's search engine (also SDK 8's 'dotnet format'; fixed in SDK 10's) queues each project's search
/// as a lambda over the loop's shared symbol list, on one exclusive scheduler for the whole process. When other searches
/// keep that scheduler busy (parallel renames, parallel tests), the work item starts late and sees symbols a later
/// project's inheritance cascade added but hasn't registered yet: KeyNotFoundException "The given key 'Derived.run()'
/// was not present in the dictionary" (about 1 search in 1,000 under load). A new search has a new engine.
/// </summary>
internal static class References
{
    private const int Attempts = 5;

    public static Task<IEnumerable<ReferencedSymbol>> FindAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken) =>
        RetryAsync(() => SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken));

    /// <summary>Runs <paramref name="search"/> until it doesn't throw <see cref="KeyNotFoundException"/>, at most <see cref="Attempts"/> times.</summary>
    internal static async Task<T> RetryAsync<T>(Func<Task<T>> search)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await search().ConfigureAwait(false);
            }
            catch (KeyNotFoundException) when (attempt < Attempts)
            {
            }
        }
    }
}
