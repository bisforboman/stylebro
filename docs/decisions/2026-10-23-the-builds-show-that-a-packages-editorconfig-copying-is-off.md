# The builds show that a package's editorconfig copying is off (2026-10-23)

## Question

Trial 46, dailydevops/codebuilder: NetEvolve.Defaults 2.10.0 copies its `configurations/template.editorconfig` over the
root .editorconfig before every build, and init says so (trials 44/45: "Turn the package's copying off, then run this
again"). With the package's own switch set (`<DisableSupportAdditionalFiles>true</DisableSupportAdditionalFiles>`), init
still printed the note and format, check and even check --build said "Not verified" (exit 3), although both TWAE builds
kept init's block and were clean (1,266/1,266 tests). `Migration.PackageCopy` only looks at the package's files, never at
whether the copy still runs. How should bro tell that the copying is off?

## Choices

1. **Builds prove it** (owner's pick): check --build compares the root .editorconfig before and after its builds;
   unchanged (init's block in it) and clean builds = the copying is off, so the gap goes away there, and that is
   remembered (keyed by the package, its file and every project and build file) so later init/format/check don't name it
   either. Until then init's note says to run `bro check --build`.
2. Run the copy target: run the package's copying target once and compare (works without a full build, but runs package
   code that rewrites other files in place).
3. Evaluate its Condition: read the copying target's Condition and evaluate it with the project's properties (static; a
   property set only during the build evaluates empty).
4. Leave it: the verdict stays Not verified with such a package.

## Decision

Builds prove it.

Applied as: `Migration.CopyIsOff`/`RememberCopyOff` (a record in the temp folder named by a hash of the package, its file
and every `.props`/`.targets`/`.csproj`/`.rsp` file's path and text, so the preview's copy finds it too and a changed
property needs new builds); `FormatCommand.CopyOffVerdict` decides after the builds. Only a root .editorconfig with init's
block counts as proof (without it, the file can be the package's own text, which a copy leaves as it is).
