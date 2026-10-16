# Microsoft style: selected with StyleBroStyle and init --style (2026-10-16)

## Question

How does a repository pick the Microsoft style?

## Choices

1. **An MSBuild property plus an init flag** (owner's pick): `<StyleBroStyle>microsoft</StyleBroStyle>` adds a style
   layer on top of the versioned preset (`StyleBroPreset`), and `stylebro-migrate init --style microsoft` sets it. The
   conventions `init` detects still win where the code clearly differs (a repository with `this.` everywhere keeps it).
2. A second preset (`<StyleBroPreset>microsoft-1.0</StyleBroPreset>`): mixes two questions (which rules exist, which
   style) into one value and doubles the preset versions.
3. Only `init --style microsoft`, writing every setting into `.editorconfig`: no package change, but the style can't
   follow StyleBro's releases (new rules) and every repository carries a long block.

## Decision

**Choice 1.** How the layer combines with preset versions, `none`, `migrate --write` and init's conventions is in
[the proposal](../proposals/microsoft-style.md).
