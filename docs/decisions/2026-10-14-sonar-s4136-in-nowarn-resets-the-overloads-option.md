# Sonar S4136 in NoWarn resets the overloads option (2026-10-14)

## Question

bcrypt.net suppresses Sonar's S4136 (overloads together) in `src/Directory.Build.props`' `<NoWarn>`. S4136 maps to an
option of BRO1001 (`stylebro_keep_overloads_together = same_access`), but its suppression was carried over like any other
Sonar suppression: `BRO1001` was added to that `<NoWarn>`, so member ordering was off for all of `src/`. How should a
suppressed Sonar rule that only sets an option of a StyleBro rule carry over?

## Choices

1. **Map it to the option**: where an MSBuild file's `<NoWarn>` has S4136, the block gets a section for that folder with
   `stylebro_keep_overloads_together = false` (StyleBro's own value); BRO1001 stays on. `#pragma`/`[SuppressMessage]` of
   such a rule add nothing (an option can't follow a pragma's lines).
2. Keep suppressing the whole rule.

## Decision

**Choice 1** (owner's choice 2026-10-14), in `init` and `--write`.
