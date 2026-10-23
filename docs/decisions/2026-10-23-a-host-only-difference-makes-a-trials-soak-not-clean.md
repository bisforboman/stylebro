# A host-only difference makes a trial's soak not clean (2026-10-23)

## Question

Trial 47 ran `bro format`, `bro check` and `bro check --build` with `--engine host` next to the default engine on all four
repositories. On SmartFormat the host's net462 run reported 23 findings the default engine didn't: MSBuildWorkspace had
sent the net462 projects' references to the libraries' netstandard2.0 instances, and the analyzers reported those
instances' copies of the files, which the whitespace formatter had left alone. The trees and exit codes were the same.
After the fix (a run's references go to the instances it names), all four repositories were identical on both engines.
Does a host-only difference count against "clean", and does trial 47 count as a clean soak?

## Choices

1. **Not a clean soak** (owner's pick): the host had a difference in this trial, so 0 clean soaks so far; the trial's
   1.0 verdict is about the default engine.
2. Clean soak after the fix: the final run with the fix was identical on all four.
3. Counts against clean: a host-only difference also makes the trial itself not clean.

## Decision

Not a clean soak. The host becomes the default after about 3 clean soaks; the count is 0 after trial 47.
