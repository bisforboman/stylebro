# Modernizing code for newer runtimes

`stylebro-migrate init --modernize` turns on the .NET SDK's own rules that rewrite code into newer C# and newer APIs:
target-typed `new()`, collection expressions, `is not null`, file-scoped namespaces, `ArgumentNullException.ThrowIfNull`,
ranges, and more. StyleBro adds no rules of its own here: the SDK has a fixer for each that `dotnet format` applies.
What `init` adds is the choice of which rules are safe in your repository, and the package adds the
[multi-target guard](#the-multi-target-guard), which keeps the newer-API rules out of projects where a target framework
lacks the API.

```
stylebro-migrate init --modernize            # shows the blocks and notes
stylebro-migrate init --modernize --write    # adds them to .editorconfig
dotnet format
```

It's opt-in: plain `init` doesn't write these rules, and StyleBro's preset doesn't turn them on. The rules go in their own
block in the root `.editorconfig`, between `# BEGIN stylebro-modernize` and `# END stylebro-modernize`. Running the
command again replaces it, and it stays when you run `init` without `--modernize` or `stylebro-migrate --write`. Keys your
root `.editorconfig` already sets for C# (a rule's severity, `csharp_style_namespace_declarations`) are left out, so
your settings stay. The CA rules need the .NET analyzers, which are on by default from .NET 5; set
`<EnableNETAnalyzers>true</EnableNETAnalyzers>` for projects that target only .NET Framework or .NET Standard.

## The tiers

| Tier | Needs | Rules | Severity |
|---|---|---|---|
| A | C# 7.3 or older, no new API | IDE0016-IDE0020, IDE0029-IDE0031, IDE0041, IDE0054, IDE0180, IDE0240, IDE0241, IDE0250, IDE0270, CA1825, CA1829, CA1834 | warning |
| B | a newer C# version | IDE0028, IDE0062, IDE0074, IDE0078, IDE0083, IDE0090, IDE0150, IDE0161 (with `csharp_style_namespace_declarations = file_scoped`), IDE0170, IDE0300-IDE0304, IDE0306, IDE0340, IDE0360 | warning, or suggestion when a multi-targeted project doesn't set `LangVersion` |
| C | a newer API | CA1510-CA1513, CA1847, CA1850, CA1864, CA1865, CA1872, CA2249, CA2263, IDE0056, IDE0057, IDE0330 | warning; the [multi-target guard](#the-multi-target-guard) hides them where a framework lacks the API |

`dotnet format` fixes warnings, not suggestions (its default `--severity` is `warn`), so a suggestion only shows up in
the IDE. The command prints a note for every tier it turned down, with the reason and how to turn it on.

## Examples

Tier A:

```csharp
// Before
if (ReferenceEquals(value, null)) { return; }   // IDE0041
int[] empty = new int[0];                       // CA1825
builder.Append("a");                            // CA1834
count = count + 1;                              // IDE0054

// After
if (value is null) { return; }
int[] empty = Array.Empty<int>();
builder.Append('a');
count++;
```

Tier B:

```csharp
// Before
namespace App
{
    class Cache
    {
        private readonly object gate = new object();   // IDE0090
        private int[] sizes = new int[0];              // IDE0300
        bool Check(object o) => !(o is string);        // IDE0083
    }
}

// After
namespace App;                                         // IDE0161

class Cache
{
    private readonly object gate = new();
    private int[] sizes = [];
    bool Check(object o) => o is not string;
}
```

Tier C:

```csharp
// Before
if (name == null)
{
    throw new ArgumentNullException(nameof(name));   // CA1510
}

var inner = text.Substring(1, text.Length - 2);      // IDE0057
var kind = (Kind)Enum.Parse(typeof(Kind), value);    // CA2263

// After
ArgumentNullException.ThrowIfNull(name);

var inner = text[1..^1];
var kind = Enum.Parse<Kind>(value);
```

## Multi-targeted projects

In a project with `<TargetFrameworks>net48;net8.0</TargetFrameworks>` every file is compiled once per framework, and
the rules run in each. They check what that compilation has: `ArgumentNullException.ThrowIfNull` exists for net8.0
and not for net48, so CA1510 reports only in the net8.0 copy, `dotnet format` applies the fix to the shared file, and
the net48 build breaks. CA1847 is worse: `Contains('a')` still compiles on net48, but binds to LINQ's
`Enumerable.Contains<char>` there. Probed on the SDK 10 analyzers: 14 of 15 API rules broke the net48 build.

The language rules have the same problem only when `LangVersion` isn't set: the SDK then picks a C# version per
framework (7.3 for .NET Framework and netstandard2.0), the rules fire only for the newer one, and the older one gets
CS8370. With `<LangVersion>latest</LangVersion>` (or any explicit value) in the project or a `Directory.Build.props`
it imports, every framework uses the same version and the rules are safe. `init` checks the project file and the
`Directory.Build.props` files it imports. Single-target projects are always safe: the rules only fire when the
project's framework and C# version have the feature.

For the API rules StyleBro has the multi-target guard, so tier C is at warning in every repository.

## The multi-target guard

`MultiTargetSuppressor` (in the StyleBro.Analyzers package) hides a tier C diagnostic when the project targets several
frameworks and one of them lacks the API the rule's fix writes. A hidden diagnostic is neither reported by the build nor
fixed by `dotnet format`: in a `net48;net8.0` project `throw new ArgumentNullException(...)` stays, while a
single-target `net8.0` project in the same repository gets `ArgumentNullException.ThrowIfNull`. Single-target projects
are left to the rule itself, which only reports when the project's framework has the API.

The package's build targets pass the project's framework list to the compiler as `StyleBroTargetFrameworks` (the
evaluated `TargetFrameworks` with `,` for `;`, so a list set in an imported `.props` counts too; every per-framework
build sees the whole list). Each rule has a minimum: the first .NET (Core) and .NET Standard version with every API its
fix can write.

| Rule | Fix writes | .NET (Core) | .NET Standard |
|---|---|---|---|
| CA1510 | `ArgumentNullException.ThrowIfNull` | 6.0 | none |
| CA1511 | `ArgumentException.ThrowIfNullOrEmpty` | 7.0 | none |
| CA1512 | `ArgumentOutOfRangeException.ThrowIfNegative`, ... | 8.0 | none |
| CA1513 | `ObjectDisposedException.ThrowIf` | 7.0 | none |
| CA1847 | `string.Contains(char)` | 2.1 | 2.1 |
| CA1850 | static `HashData` (SHA/MD5 5.0, HMAC 6.0) | 6.0 | none |
| CA1864 | `Dictionary.TryAdd` | 2.0 | 2.1 |
| CA1865 | `StartsWith(char)`, `EndsWith(char)`, `IndexOf(char, StringComparison)` | 2.1 | 2.1 |
| CA1872 | `Convert.ToHexString` (5.0), `ToHexStringLower` (9.0) | 9.0 | none |
| CA2249 | `string.Contains(string, StringComparison)` | 2.1 | 2.1 |
| CA2263 | generic overloads (`Enum.Parse<T>` 2.0, `Enum.GetValues<T>` 5.0) | 5.0 | none |
| IDE0056 | `a[^1]` (`System.Index`) | 3.0 | 2.1 |
| IDE0057 | `s[1..^1]` (`System.Range`) | 3.0 | 2.1 |
| IDE0330 | `System.Threading.Lock` | 9.0 | none |

.NET Framework has none of them, and a framework the guard doesn't recognize (`monoandroid`, `uap`, ...) counts as
lacking everything. A minimum is the newest API the fix can write, so some safe fixes are hidden too (CA1872 in a
`net8.0;net9.0` project, where only the lowercase variant needs .NET 9).

What it doesn't see:

- CA2263 covers any method with a generic overload; the table knows the base library's. A package whose generic
  overload exists for only some of the project's frameworks isn't known.
- Projects without the StyleBro.Analyzers package get no guard.
- It works per build: a framework added later hides the rules again, one removed lets them fix.
- To turn it off for a project, set `StyleBroTargetFrameworks` to a single framework (that is how
  `scripts/verify-package.ps1` checks that the rules do fire there without it).

## A second run

Some fixes produce code another rule then rewrites, and `dotnet format` applies them in one pass in no fixed order:
CA2249 writes `s.Contains("x", StringComparison.CurrentCulture)`, which CA1847 turns into `Contains('x', ...)` on the
next run; with every rule on, IDE0041 was once left for the next run; fixers write new blocks on one line, which the
formatter wraps on the next run. Run `dotnet format` until a run changes nothing (usually two).

## Left out

Opt-in by hand (add `dotnet_diagnostic.<id>.severity = warning` outside the block), debated or with side effects:

| Rule | Why not by default |
|---|---|
| IDE0290 | Primary constructors: debated style; fields become captured parameters. |
| IDE0066 | Switch statement to switch expression: debated readability. |
| IDE0063 | Simple `using` declaration: changes when the object is disposed (end of the scope). |
| IDE0305 | `x.ToList()` to `[.. x]`: debated readability. |
| IDE0251 | `readonly` struct members: together with IDE0250 (readonly struct) in one run it adds a redundant `readonly` to members of the now-readonly struct. Turn it on after a run with IDE0250 if you want it. |

Skipped:

| Rule | Why |
|---|---|
| CA1866, CA1867 | No fixer (the SDK's fixer handles CA1865 only), so `dotnet format` can't apply them. |
| IDE0038 | No Fix All, so `dotnet format` can't apply it. |
| CA1845, CA1846 | Span-based performance rewrites, not modernization; they overlap IDE0057. |
| IDE0230 | UTF-8 string literals: niche, and the rewrite (`"ab"u8.ToArray()`) isn't nicer. |
| IDE0320 | `static` lambdas: noise. |
| CA1860 | `Any()` to `Count != 0`: many find `Any()` clearer. |
