# BRO1524 mirrors the other token when BRO1520 is off (2026-10-17)

## Question

`init` turns BRO1520 off when a repository's code puts operators on both sides of a wrapped line (NLog). BRO1524 still
put a missing line break on the side `dotnet_style_operator_placement_when_wrapping` asks for (beginning of the line by
default), so `var x = cond ?` + newline + `a : b;` became `a` + newline + `: b`: one conditional with `?` at the end of
a line and `:` at the start of the next (NLog's LoggerImpl.cs:79). Which side should BRO1524 pick when BRO1520 is off?

## Choices

1. **Mirror the other token**: the missing line break goes on the side the conditional's other token already has its
   line break on, so each conditional is consistent on its own.
2. Keep following the setting (today): consistent with the setting, but mixed conditionals in exactly the repositories
   where nothing enforces the setting.
3. Report nothing when BRO1520 is off.

## Decision

**Choice 1** (owner's choice 2026-10-17: "mirror the other token"). Only when BRO1520 is off (severity `none`, or not
on): with BRO1520 on, the line break goes where the setting asks and a token on the wrong side stays BRO1520's. When
the other token has line breaks on both sides, its side isn't clear and the setting decides.
