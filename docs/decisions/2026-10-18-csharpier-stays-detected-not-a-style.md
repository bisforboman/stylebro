# CSharpier stays detected, not a style (2026-10-18)

## Question

Should CSharpier become a style, `<StyleBroStyle>csharpier</StyleBroStyle>`, next to `stylecop` and `microsoft`?

## Choices

1. **Keep detection and document it** (owner's pick). `init` already notices CSharpier (a `.csharpierrc`, the
   `csharpier` dotnet tool or the CSharpier.MsBuild package; [CSharpier in init](2026-10-11-csharpier-in-init.md)),
   turns off the rules for the layout CSharpier owns, and `bro format` runs CSharpier after its fixes. The styles page
   says so.
2. Detection, plus `init --csharpier` to force it where CSharpier leaves no trace in the repository (run only in CI as
   a global tool).
3. A `csharpier` style: a layer that turns the layout rules off and sets `stylebro_layout_by`.
4. Leave it as it is.

## Decision

Detection, documented on [styles.md](../styles.md). A style doesn't fit, for three reasons:

- `<StyleBroStyle>` holds one value, but CSharpier is about who formats the layout, not which conventions the code
  follows. A repository can follow the Microsoft style and use CSharpier; as a style it would have to pick one.
- The settings that stop the two formatters undoing each other include IDE0055 and IDE0036 off, and `dotnet format`
  reads rule severities only from `.editorconfig`, never from a global config. A layer couldn't hold them, so `init`
  would still write them and the settings would sit in two places.
- CSharpier often formats only some folders; `init` writes those settings per folder section, which one MSBuild
  property can't say.
