using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// The rename-safety checks of one solution, each made once. 'dotnet format' registers the fix of one diagnostic only to
/// read the action's equivalence key, and the registration runs the check to offer no action for a kept rename; Fix All
/// then asked the same question again on the same solution (Shouldly: BRO1311's registration 8.3 s, its Fix All 4.1 s).
/// The checks only read the solution, so a result is the same whoever asks first.
/// </summary>
internal static class RenameChecks
{
    /// <summary>Tests only: counts the checks made (not taken from the cache) in this async flow while set.</summary>
    internal static readonly AsyncLocal<StrongBox<int>?> Made = new();

    private static readonly ConditionalWeakTable<Solution, ConcurrentDictionary<string, Lazy<Task<object>>>> Cache = new();

    /// <summary>
    /// The check's result for <paramref name="key"/> in <paramref name="solution"/>, computed once. A check canceled by the
    /// caller that started it is made again for the next caller.
    /// </summary>
    public static async Task<T> GetAsync<T>(Solution solution, string key, Func<CancellationToken, Task<T>> check, CancellationToken cancellationToken)
    {
        var checks = Cache.GetValue(solution, _ => new ConcurrentDictionary<string, Lazy<Task<object>>>(StringComparer.Ordinal));
        while (true)
        {
            var entry = checks.GetOrAdd(key, _ => new Lazy<Task<object>>(async () =>
            {
                if (Made.Value is { } made)
                {
                    Interlocked.Increment(ref made.Value);
                }

                return (await check(cancellationToken).ConfigureAwait(false))!;
            }));
            try
            {
                return (T)await entry.Value.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                ((ICollection<KeyValuePair<string, Lazy<Task<object>>>>)checks).Remove(new(key, entry));
            }
        }
    }
}
