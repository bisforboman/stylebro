# StyleCop's defaults named as the stylecop style (2026-10-18)

## Question

With `microsoft` as a style, should StyleCop's conventions be a style too, instead of being the preset's built-in
default?

## Choices

1. **Name the default** (owner's pick): `<StyleBroStyle>stylecop</StyleBroStyle>` and `init --style stylecop` are an
   explicit name for what the preset already is. No layer of its own, nothing in preset 1.0 moves; `none` and unset mean
   the same.
2. A style-neutral base preset with `stylecop` and `microsoft` both as layers: changes what `StyleBroPreset=1.0` means
   for every user, moves keys between layers and revisits `migrate --write` and `none`; it reopens the 2026-10-16 "no
   second preset" decision and would reset the clean-trial tally toward 1.0.
3. No change: StyleCop stays the implicit base, `microsoft` the only named style.

## Decision

**Choice 1.** The package accepts `stylecop` (STYLEBRO003 lists it, STYLEBRO004 warns for it with
`<StyleBroPreset>none</StyleBroPreset>` like for `microsoft`); `init --style stylecop` pins the name in
Directory.Build.props and writes exactly what plain `init` writes. `migrate --write` still removes `<StyleBroStyle>`.
