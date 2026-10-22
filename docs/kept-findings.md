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
- the name is in a snapshot or approval file (`*.verified.*`, `*.received.*`, `*.approved.*`), which a test compares:
  what a serializer wrote, or a failure message quoting the code; in a project using Shouldly, also in a string of the same
  file (an expected Shouldly message, which quotes the call site);
- a XAML file uses the name (a binding, an event handler, or a type or property of the project's own namespace);
- the field registers a XAML framework's property or routed event (`LevelProperty` for an Avalonia, WPF, WinUI or MAUI
  property `Level`, `ClickEvent`): XAML and the framework's analyzers find it by that name;
- a derived type hides the member with a `new` member of the same name, which would then hide nothing (CS0109);
- another member would hide the new name, or the new name already means something else where the name is used;
- a use is in generated code (a source generator, a Razor page, a designer file), which a tool writes again;
- a related override or implementation is public API, or also implements a member that isn't renamed;
- the rename would add compile errors (tuple element names), or the namespace is also defined outside the solution;
- a documentation fix (BRO1603's period, BRO1604-BRO1607's standard words) would change text that is a string, or is in a
  snapshot file, in the solution: code that reads XML docs at run time (NSwag's or Swashbuckle's OpenAPI descriptions) and
  a test comparing the text. Texts of one word, and elements holding more than text (`<see cref="..."/>`), don't count.

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
| 3 | nothing left to change in what it checked, but part of the build wasn't checked (`Not checked:` lines, `Not verified:`) | the same |
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

## Not verified

`format` and `--verify-no-changes` never say "Clean" for code they didn't see the way the build sees
it. Each part they couldn't check gets a `Not checked: <what>: <why>` line before the verdict, which then is
`Not verified: N parts of the build weren't checked (...); the rest is clean.` with exit code 3:

- a project `dotnet format` couldn't load (references didn't load, a reference to an F# project), in every run or for
  some target frameworks (`Incomplete:` / `Partly incomplete:` above it);
- a project that needs a workload this machine doesn't have (Android, iOS, ...);
- a project the build compiles that the solution doesn't list, which no run loads (a multi-targeted one), or which only
  the default configuration's run formats while its `#if` code depends on the configuration;
- configurations past the four other ones format runs per project;
- the configurations of projects with an in-solution source generator, which only a build checks (`format` only:
  `--verify-no-changes` builds them);
- files a pass leaves out (a CR alone in a string), and rules whose fixes can't run: xunit.analyzers 1.24 and later
  (no Fix All), IDE0005 while files the projects compile are missing.

IDE fixes held back where this SDK's `dotnet format` ignores the multi-target guard (see
[Modernizing](modernizing.md)) are listed with a `Not checked:` line too, but only as a note: on their own they leave
the verdict clean and the exit code 0. Each part listed above still makes it "Not verified".

Skips the build also makes aren't gaps: git submodules, Visual Basic projects, generated files, files outside the
repository, projects no solution configuration builds, whitespace where IDE0055 is off. `--json` lists the gaps as
`notChecked`. To see what the build itself reports there, [`bro check --build`](ci.md#build-every-configuration).
