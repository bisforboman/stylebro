# Proposal: namespace names (SA1300 for namespaces)

Status: **decided 2026-10-04: ship opt-in** as recommended below (see [decisions.md](../decisions.md)). BRO1312 is
off by default, `stylebro-migrate` turns it on with SA1300, the root namespace isn't reported, public namespaces are
renamed, and the resource, config and XAML limits are documented.

BRO1309 replaces StyleCop's SA1300 for types and members but skips namespaces, so `namespace myCompany.data;` is
not reported. This page collects what StyleCop does, everything a namespace rename can affect (with experiments),
how common the case is, and a prototype rule, **BRO1312**, that reports and renames namespace parts with guards.

## What StyleCop does

Source read from `SA1300ElementMustBeginWithUpperCaseLetter.cs` and `RenameToUpperCaseCodeFixProvider.cs`
(DotNetAnalyzers/StyleCopAnalyzers, master):

- **Analyzer.** Every namespace declaration, block and file-scoped (`BaseNamespaceDeclaration`). The name is split
  recursively at each dot and **every part is checked on its own**: `namespace myCompany.data` gets two diagnostics,
  and every file that declares it gets them again. A part is reported when its first character is lower-case
  (`char.IsLower`) or `_`. Parts listed in stylecop.json's `namingRules.allowedNamespaceComponents` (for brand names
  like `eBay`) are skipped. `using` directives, aliases and qualified names are not checked: only declarations.
- **Fix.** "Namespaces are not symbols. So we are just renaming the namespace": the fix replaces that one identifier
  in that one declaration (leading `_` dropped, first letter upper-cased) and nothing else. Other files' declarations
  and every `using myCompany.data;` stay behind, so the code stops compiling. The parity set `naming-namespaces`
  confirms it under `dotnet format`: StyleCop's output fails with CS0246 in the file that has the `using`.

## What a rename can affect

Experiments ran on .NET SDK 10.0.401 with a small solution: a library `myCompany.data.csproj` (so `RootNamespace` and
`AssemblyName` are `myCompany.data`) with a plain embedded resource, three `.resx` files (one with a designer file
in the shape of ResXFileCodeGenerator's output, one next to a same-named class, one without code) and a console
app that reads them all. The namespace declarations and the app's `using` were then renamed to `MyCompany.Data`.

| What | Affected? | Evidence |
|---|---|---|
| Other files' namespace declarations, `using`, `global using`, `using static`, aliases, `global::` names, qualified names, crefs | Yes, all must change together | Unit test `EveryDeclarationAndReference_IsRenamed` (prototype). StyleCop's fix breaks the build here. |
| `nameof(myCompany.data)` | The value changes (`"data"` to `"Data"`) | Same as renaming a type used in `nameof`. |
| Embedded resource manifest names (plain `EmbeddedResource`) | Not renamed: they come from `RootNamespace` + folder path, not from the code | Manifest stayed `myCompany.data.data.json`; `GetManifestResourceStream(typeof(Foo).Namespace + ".data.json")` returned null after the rename. **Silent run-time break.** |
| `.resx` read through `ResourceManager` | Survives a case-only rename | `new ResourceManager(typeof(Foo).Namespace + ".Other", asm)` and `new ResourceManager(typeof(Strings))` still found `myCompany.data.*.resources`: ResourceManager falls back to a case-insensitive lookup. A rename that also drops a `_` would break them. |
| `.resx` next to a same-named class (`Widget.resx` + `Widget.cs`) | Manifest name follows the class's namespace (the SDK's DependentUpon convention) | Manifest became `MyCompany.Data.Widget.resources`; `typeof(Widget)` still matches. |
| resx designer file (`Strings.Designer.cs`) | Hard-codes `"myCompany.data.Strings"` and is regenerated from the project's namespace | Kept working only because the string wasn't touched; regenerating it writes the old casing back. |
| `RootNamespace` (default: the project name) | Not changed by a code rename; new files and regenerated code get the old casing back | `build_property.RootNamespace = myCompany.data` is visible to analyzers (generated .editorconfig). |
| Strings with the name: `Type.GetType("myCompany.data.Foo, myCompany.data")`, Json.NET `$type`, BinaryFormatter, DataContract default XML namespaces, DI or logging configuration by type name | Yes, case-sensitive | `Type.GetType` with the old name returned null after the rename. Stored data (JSON with `$type`, config files, other repositories) can't be seen at all. |
| The SDK's generated `AssemblyInfo.cs` | Contains the assembly name as strings (`AssemblyTitle("myCompany.data")`) | Found in the end-to-end run: a naive "any string contains the name" guard blocked every rename of a project's own namespace. The prototype ignores strings in generated code. |
| `[assembly: InternalsVisibleTo]`, `extern alias` | No | They name assemblies, not namespaces. |
| Razor (`@using`, `@namespace`) | Yes, and the fix can't edit `.razor` files | After the rename, `Hello.razor(1,8): error CS0246: 'myCompany' could not be found`. `.razor` files are AdditionalFiles, so the fix can read them. |
| XAML (`x:Class`, `clr-namespace:`) | Yes | Not run here (no WPF workload). `x:Class` produces a generated `.g.cs` declaration (skipped by the prototype if the workspace has it); a bare `clr-namespace:` reference is invisible to the fix. |
| Source generators | References in generated trees can't be edited; generators that write `RootNamespace` keep the old casing | The prototype checks every tree of the compilation, generated ones included. |
| Public API | Every type in the namespace gets a new full name: a breaking change for consumers | Same as BRO1309 renaming a public type, but for every type at once. |
| A namespace spanning several projects | All projects must change together | Unit test `AProjectReference_IsRenamedWithIt`; in the end-to-end run the app's `using` was renamed from the library's diagnostic. |
| A namespace also declared by a package (`myCompany.common` from NuGet) | Can't be renamed: the package keeps it | Skipped (unit tests for both the reporting project and another project of the solution). |
| A namespace differing only in case (`MyCompany` and `myCompany` both exist) | Renaming merges them; types can collide | Skipped. |
| Folders (IDE0130) | No new IDE0130 warnings | IDE0130 compares case-insensitively: after the case-only rename it stayed quiet, while a planted `namespace Totally.Different` got `expected "myCompany.data"`. `dotnet format` can't rename folders anyway. |

## How common is it?

Namespace declarations in 22 public repositories (the 8 real-world reference repos plus 14 popular ones; source
tarballs, every `.cs` file):

| Repositories | Files with a lower-case namespace part | What they are |
|---|---|---|
| FFMpegCore, Polly, OpenTelemetry, Newtonsoft.Json, Serilog, Jellyfin, FluentValidation, CsvHelper, AutoMapper, Dapper, MediatR, EF Core, NodaTime, ShareX, Files, ... (16 of 22) | 0 | |
| itext-dotnet | 3,828 of 3,863 | `iText` (brand) |
| Humanizer | 185 | culture codes in test namespaces (`Humanizer.Tests.Localisation.ur`) |
| Avalonia | 45 | `iOS` (brand) |
| duplicati | 15 | `pCloud`, `iDrive` (brands), one `secrets` |
| Nancy | 5 | `xUnitExtensions` (brand) |
| ILSpy, PowerToys | 4 | decompiler test output, a `__MODULE__` template placeholder |

Every lower-case part found was deliberate: brand names, culture codes, generated or template code. Not one looked
like a mistake. The StyleCop-enforcing repos (Polly, OpenTelemetry, Jellyfin) have none, which is what SA1300
guarantees; for them the rule's value is catching a new one.

## The prototype: BRO1312

A separate id instead of more BRO1309: the risk is of a different kind (every type in the namespace gets a new full
name), so it should be possible to have BRO1309 on and this off. **Off by default** in the descriptor and the preset.
`stylebro-migrate` turns it on when SA1300 is on (it replaces SA1300 for namespaces) and carries
`allowedNamespaceComponents` over as `stylebro_allowed_namespace_components`.

Reported like StyleCop: each part of every declaration. Not reported (the analyzer):

- parts in `stylebro_allowed_namespace_components`;
- the new name already exists in the containing namespace (a namespace or type `MyCompany`), from any assembly;
- the namespace, or one inside it, also comes from another assembly (a package, or another project that this project
  references: the project that sees only its own part reports, and the fix renames all projects);
- the namespace is declared in generated code (`.designer.cs`, `.g.cs`, `<auto-generated>`);
- **the project's `RootNamespace` is the namespace or inside it** (`build_property.RootNamespace`). Resource names,
  new files and regenerated code follow the root namespace, so the code-only rename breaks plain embedded resources
  and doesn't last. The user renames the project or sets `RootNamespace`; the declarations then follow on the next
  run (end-to-end run below).

The fix (`NamespaceRenamer`, applied by the shared renamer so a namespace and its types rename in one pass) works
on the whole solution, all or nothing. It renames every identifier whose namespace symbol has the old full name: the
declaration parts (found from the declared symbol, walking up one level per part to the right) and every reference
(`GetSymbolInfo`), crefs included. Skipped, the warning staying (the documented exception, like BRO1309's string guard):

- a string literal in the solution's own (non-generated) code, or an additional file, contains the full name;
- disabled `#if` code contains the part;
- a reference sits in generated code or a source generator's output;
- another project gets part of the namespace from an assembly outside the solution;
- the new name exists in another project's containing namespace;
- the new name is in scope where a reference looks the part up by its simple name;
- code inside the containing namespace uses the new name for a type, namespace or alias (it would find the renamed
  namespace first). This one is conservative: a nested type with the new name also blocks.

Files and folders are not renamed.

### Results

- Unit tests: 15 in `NamespaceNamingTests` (single fix and Fix All, multi-file, two projects, metadata, generated
  code, additional files, each guard), plus a migration test. `dotnet test StyleBro.slnx`: 517 passed.
- Mutation testing: 14 entries in `scripts/mutation/mutations.psd1`, all killed.
- Parity set `naming-namespaces`: StyleCop 7 / StyleBro 6 positions; the one difference is `taken` next to `Taken`.
  StyleCop's fixed output doesn't compile (the `using` in the other file stays lower-case); StyleBro's does.
- `scripts/verify-format.ps1` with a Messy case (`Catalog.cs`, BRO1312 on for that file): passes, second run clean.
- Self-check `dotnet format StyleBro.slnx --verify-no-changes --severity warn --exclude samples`: clean.
- End-to-end with `dotnet format` on the experiment solution (library + app):
  1. Default `RootNamespace` (`myCompany.data`): nothing reported, nothing changed.
  2. `RootNamespace` set to `MyCompany.Data`, resx designer file present: nothing reported (generated declaration).
  3. Designer file removed, `Type.GetType("myCompany.data.Foo, myCompany.data")` in the app: reported, not fixed
     (string guard). Note that in this state, before any rename, the plain resource was already broken by the
     `RootNamespace` change (manifest `MyCompany.Data.data.json`, code asked for `myCompany.data.data.json`).
  4. String removed: library and app renamed in one run (the app's `using` too, though only the library reported),
     builds, every resource lookup works, second run clean.
- Not run: the real-world script on a repository with lower-case namespaces. The reference repos have none; the ones
  that do are brand names (they'd use `stylebro_allowed_namespace_components`), Humanizer doesn't build on this
  machine, and Avalonia's `iOS` projects need mobile workloads.

## Open questions for the owner

1. **Ship it at all?** In 22 repositories every lower-case part was intentional. The rule's main value is for
   StyleCop migrants: without it, migrating loses SA1300's check on new namespaces.
2. **Separate id (BRO1312) and off by default?** The prototype says yes to both. BRO1311 is taken by the tuple
   element names branch, hence 1312.
3. **Should `stylebro-migrate` turn it on when SA1300 is on?** It does now (the description says it replaces SA1300).
   For a StyleCop-clean repo it reports nothing, as long as the allowed components carry over (they do).
4. **The root namespace: not reported, or reported but not fixed?** Not reporting keeps "every diagnostic has a fix",
   but a lower-case project name then goes unnoticed. Reporting it would be a diagnostic `dotnet format` can't fix.
5. **Rename public namespaces in libraries?** It renames every public type. Options: always (like BRO1309), never
   when the namespace has a public type, or only in projects that aren't packable.
6. **Resources in a folder named like a lower-case part.** Their manifest names use the folder's casing; only code
   that builds the name from `typeof(T).Namespace` breaks. The analyzer can't see `EmbeddedResource` items. The
   package's targets could expose "this project has embedded resources" as a compiler-visible property and the rule
   could skip such projects. Worth it?
7. **Config files and stored data** (appsettings, XAML `clr-namespace:` without `x:Class`, JSON with `$type`) are
   invisible to any analyzer. Is documenting it enough?

## Recommendation

Ship BRO1312 as an **opt-in** rule (off in the preset, on through `stylebro-migrate` when SA1300 is on), with the
prototype's guards, not reporting the root namespace (question 4), and renaming public namespaces like BRO1309 renames
public types (question 5), with the breaking-change note on the rule page. It closes the parity gap for StyleCop
users at little risk: StyleCop-clean repos have nothing to rename, and a new lower-case namespace is usually a single
declaration caught early. If parity for this rarely-hit case isn't worth the extra rename code (about 350 lines), the
alternative is to keep the skip and record it in `skipped.psd1` with the findings above.
