# Findings kept on purpose

Every StyleBro finding has a fix, but a fix can see more than the analyzer: the whole solution, other projects
included. When a change would break something only the fix can see, it leaves the finding, the warning stays, and the
fix offers no code action for it. That's the one exception to "`dotnet format` fixes everything StyleBro reports",
and it's on purpose. The reasons, per finding:

- the name is in a string elsewhere in the solution (serialized names and test JSON for properties and public fields;
  for other fields only where code reaches a member by name: `GetField("_count")`, `GetProperty(...)`, `Type.GetType(...)`,
  `[MemberData("...")]`, an attribute's `Name = "..."`, or anywhere for a name only a field has, like `"_count"`): the
  rename would compile but break at run time;
- the name is in a `nameof(...)` elsewhere, whose text would change with it (an HTTP header name, a log key);
- the name is a structured log key: it's in an interpolated string's hole whose handler takes the hole's text as a
  `[CallerArgumentExpression]` argument (ZLogger's `$"{UserId}"`);
- a XAML file uses the name (a binding, an event handler, or a type or property of the project's own namespace);
- the field registers a XAML framework's property or routed event (`LevelProperty` for an Avalonia, WPF, WinUI or MAUI
  property `Level`, `ClickEvent`): XAML and the framework's analyzers find it by that name;
- a derived type hides the member with a `new` member of the same name, which would then hide nothing (CS0109);
- another member would hide the new name, or the new name already means something else where the name is used;
- a use is in generated code (a source generator, a Razor page, a designer file), which a tool writes again;
- a related override or implementation is public API, or also implements a member that isn't renamed;
- the rename would add compile errors (tuple element names), or the namespace is also defined outside the solution.

`bro format` lists them after its last run:

```
Run 2: 0 files changed, clean.
Clean: 1 finding kept on purpose (listed below). StyleBro's fixes leave these: renaming by hand breaks what the check protects. Check those uses first, or suppress or baseline them (...).
  src/Counter.cs(5,17): BRO1303 Rename '_count' to 'count'. Kept: the name is in a string in the solution (reflection, serialization): renaming would compile but break at run time.
```

What to do with one: **don't rename by hand to silence it** (that's what breaks the reflection or the generated code).
Check the uses the reason names; if they can change too, rename both by hand. Otherwise suppress it with a reason
(`#pragma warning disable BRO1303 // read by reflection in Tests`), or record it in the [baseline](baseline.md).

**The baseline.** When a run ends clean with findings kept, `format` records them in the baseline the build reads
(`stylebro.baseline`: the one `<StyleBroBaseline>` names, else the nearest one above, else a new one in the folder it
runs on) and says so (`Baseline: stylebro.baseline: added 3 kept findings, ...`). The build, the IDE and
`--verify-no-changes` then skip them, also with `TreatWarningsAsErrors`, while new violations still fail; commit the
file with the formatting. Its other entries stay, except those whose line is gone. Delete an entry to see its finding
again; `<StyleBroBaseline>none</StyleBroBaseline>` turns this off, and `format` and `--verify-no-changes` then say
that the build still reports them (an error with `TreatWarningsAsErrors`). The list of kept findings is printed after
every run, also when `format` ends "Not clean" (then nothing is baselined).

Exit codes of `bro format`, for CI and agents:

| Exit code | Plain run | `--verify-no-changes` |
|---|---|---|
| 0 | clean: the last run changed nothing (kept findings don't count) | formatting would change nothing; only kept findings are left |
| 2 | still changing after three runs | formatting would change a file |
| 1 | | a project no run loaded (`Incomplete:`): its files weren't checked |
| other | `dotnet format` failed (load or build errors) | the same |

`--verify-no-changes` changes nothing: when `dotnet format --verify-no-changes` reports findings, it formats the files
with findings (for their rules) in a temporary copy of the repository to see whether any file would change and which
findings the fixes keep, then deletes the copy. After a clean `bro format` on the same code, settings,
options, SDK and StyleBro version, it already knows what that run found (recorded in the temporary folder): when that
run's last step checked every target framework and configuration and found nothing but kept findings, it answers from
the record in seconds (`Checking StyleBro's rules: nothing changed since the last clean 'bro format'
here ...`) without running `dotnet format` again; otherwise it checks, and formats no copy for the kept findings it
knows. Delete the record (`stylebro-kept` in the temporary folder) to check anyway. The builds of the configurations an
in-solution source generator keeps from the checks still run: there the build is the check. The projects of one
configuration build together, in one build (a shared project once).
Plain `dotnet format --verify-no-changes` can't tell the two apart and fails on kept findings.
