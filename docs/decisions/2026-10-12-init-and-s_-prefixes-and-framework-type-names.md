# init and `s_` prefixes and framework type names (2026-10-12)

## Question

actions/runner follows the .NET runtime's style: private static fields start with `s_` (`t_` for thread-static ones), and
much of its code writes `String` and `Int32`. `init` detected neither, so BRO1307 renamed ~541 `s_` statics and IDE0049
rewrote `String` to `string` 6,424 times in 501 files. Should `init` detect these two conventions?

```csharp
private static readonly object s_lock = new();   // BRO1307: Lock
public String Name => String.Empty;               // IDE0049: string, string.Empty
```

## Choices

1. **Detect both**: count private static fields with and without `s_`/`t_`, and files with and without framework type
   names, with the usual thresholds and verdicts.
2. Detect only the prefixes.
3. Leave both to the user.

## Decision

**Choice 1** (owner: "detect both").

- **`s_`/`t_` prefixes** (unit: private static fields, constants aside): mostly with the prefix or mixed, BRO1307 is off;
  mostly without, it stays on. BRO1307 has no switch for static fields alone, and a generated `dotnet_naming_rule` that
  requires `s_` (which BRO1307 already respects) would also make IDE1006 suggest `s_` for constants and `static readonly`
  fields, so the rule goes off; repositories with `s_` statics rarely use `m_`. A naming rule of the repository's own that
  requires `s_` or `t_` decides instead (`set`).
- **Type names** (unit: files, not places: every `int` would outvote the few `String`s): files that name a built-in type
  only with keywords, against files with a framework name IDE0049 would rewrite (`String`, `System.Int32`; not `x.String`
  or `Other.String`). Mostly keywords leaves IDE0049 on; mostly framework names or mixed turns it off. The repository's own
  `dotnet_style_predefined_type_*` keys or IDE0049 severity decide instead.

On actions/runner: 127 of 194 statics with a prefix and 495 of 795 files with framework names, so both are off; the
format run then renamed no `s_` field and rewrote no `String`. Much of both comes from `src/Sdk`, code copied in under a
name `init` can't recognize as vendored: marked `generated_code = true` in the root `.editorconfig`, it no longer counts
(46 of 51 statics without a prefix in the rest).
