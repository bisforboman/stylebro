# A package whose build targets name its editorconfig counts as copying it (2026-10-22)

## Question

Trial 45, dailydevops/guard: NetEvolve.Defaults (2.0.0) copies its `configurations/template.editorconfig` over the root
.editorconfig before every build, but guard's committed root file is an older version of that template (31 lines differ:
the build rewrites it, nobody commits the result). Trial 44's check (the root file, init's block aside, equal to a restored
package's `*editorconfig` file) missed it: format and check said Clean, the first TWAE build reported 214 BRO findings
(init's block gone). How should init/format/check recognize such a package?

## Choices

1. **Targets name the file** (owner's pick): a restored package that ships an `*editorconfig` file and whose build
   `.props`/`.targets` name that file's name counts, whatever the root file holds now. Catches stale copies; a package that
   only copies when the file is missing would also count (a false Not verified; none seen so far).
2. Also compare loosely: keep the content check but accept a copy sharing most lines with the package's file.
3. check --build notices: leave detection as is; check --build reports a build that changed the root .editorconfig.

## Decision

Targets name the file.
