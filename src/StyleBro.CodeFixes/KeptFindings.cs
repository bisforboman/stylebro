using System;
using System.IO;
using Microsoft.CodeAnalysis;
using StyleBro.Analyzers;

namespace StyleBro.CodeFixes;

/// <summary>
/// Writes the findings a fix leaves on purpose to the file <see cref="KeptFinding.Variable"/> names, for
/// 'stylebro-migrate format' to list with their reasons. Does nothing when the variable isn't set (the IDE, plain
/// 'dotnet format').
/// </summary>
internal static class KeptFindings
{
    private static readonly object Gate = new();

    public static void Record(Diagnostic diagnostic, KeptReason reason)
    {
#pragma warning disable RS1035 // Only 'stylebro-migrate format' sets the variable; the IDE and the build never write.
        var file = Environment.GetEnvironmentVariable(KeptFinding.Variable);
        if (string.IsNullOrEmpty(file))
        {
            return;
        }

        var span = diagnostic.Location.GetLineSpan();
        var line = new KeptFinding(span.Path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, diagnostic.Id, reason, diagnostic.GetMessage()).ToString();
        try
        {
            lock (Gate)
            {
                File.AppendAllText(file, line + "\n");
            }
        }
        catch (IOException)
        {
            // Listing a kept finding is a courtesy: never fail the fix over it.
        }
#pragma warning restore RS1035
    }
}
