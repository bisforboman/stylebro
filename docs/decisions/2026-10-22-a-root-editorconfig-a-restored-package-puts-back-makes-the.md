# A root .editorconfig a restored package puts back makes the verdict Not verified (2026-10-22)

## Question

Trial 44, dailydevops/arguments: the NetEvolve.Defaults NuGet package (2.8.51; 2.10+ has an opt-out property) copies its
template over the root .editorconfig AND .gitignore before every build. So init's block is gone after the first build, and
its .gitignore ignores `**/.editorconfig`, which rules out trial 40's folder-file fix. `bro check` said Clean while the
build reported 610 findings. What should StyleBro do when the root .editorconfig matches a file in a restored package?

## Choices

1. **Detect, warn, Not verified** (owner's pick): init/migrate still write the block but say which package overwrites it
   and how to stop that (its opt-out, or removing the package); format/check/preview add a "Not checked: the build puts
   <package>'s .editorconfig back" gap, so the verdict is Not verified (exit 3), not Clean.
2. Detect and refuse to write: init/migrate write nothing and explain why; format/check say Not verified as above.
3. Leave it as is: an unusual package setup; note it on the docs page.

## Decision

Detect, warn, Not verified.
