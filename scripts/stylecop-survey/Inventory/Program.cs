// Lists every diagnostic a StyleCop.Analyzers package reports, and which of them its code fix providers can fix.
// Usage: Inventory <folder with StyleCop.Analyzers.dll> <output csv>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

var dir = args[0];
AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
{
    var path = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
    return File.Exists(path) ? Assembly.LoadFrom(path) : null;
};

var analyzers = Assembly.LoadFrom(Path.Combine(dir, "StyleCop.Analyzers.dll"));
var codeFixes = Assembly.LoadFrom(Path.Combine(dir, "StyleCop.Analyzers.CodeFixes.dll"));

var fixable = new HashSet<string>();
foreach (var provider in Instantiate<CodeFixProvider>(codeFixes))
{
    fixable.UnionWith(provider.FixableDiagnosticIds);
}

var rows = new SortedDictionary<string, string>(StringComparer.Ordinal);
foreach (var analyzer in Instantiate<DiagnosticAnalyzer>(analyzers))
{
    foreach (var d in analyzer.SupportedDiagnostics.Where(d => !rows.ContainsKey(d.Id)))
    {
        rows[d.Id] = string.Join(
            ",",
            Csv(d.Id),
            Csv(d.Title.ToString()),
            Csv(d.Category.Replace("StyleCop.CSharp.", string.Empty)),
            d.IsEnabledByDefault,
            d.DefaultSeverity,
            fixable.Contains(d.Id),
            Csv(d.HelpLinkUri ?? string.Empty));
    }
}

var csv = new StringBuilder("Id,Title,Category,EnabledByDefault,DefaultSeverity,StyleCopHasFix,HelpLink\n");
foreach (var row in rows.Values)
{
    csv.Append(row).Append('\n');
}

File.WriteAllText(args[1], csv.ToString());
Console.WriteLine($"{rows.Count} diagnostics, {rows.Keys.Count(fixable.Contains)} with a StyleCop code fix -> {args[1]}");

static IEnumerable<T> Instantiate<T>(Assembly assembly)
    where T : class
{
    Type?[] types;
    try
    {
        types = assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException e)
    {
        types = e.Types;
    }

    return types
        .Where(t => t is not null && !t.IsAbstract && typeof(T).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) is not null)
        .Select(t => (T)Activator.CreateInstance(t!)!);
}

static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
