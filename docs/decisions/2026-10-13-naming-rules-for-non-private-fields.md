# Naming rules for non-private fields (2026-10-13)

## Question

BRO1303/BRO1306 read the SDK's naming rules (`dotnet_naming_rule.*`) only for private fields. YamlDotNet's
`instance_fields_should_be_camel_case` covers protected, internal, protected internal and private protected instance
fields, its code follows it, and BRO1306 renamed internal fields to PascalCase (trial 13). Should a naming rule decide
non-private fields too?

## Choices

- Keep StyleCop's defaults for non-private fields (PascalCase for public/internal, camelCase for protected) whatever
  the naming rules say.
- The first naming rule that covers the field (Roslyn's order) decides: its style when StyleBro writes it (camelCase,
  _camelCase, PascalCase), otherwise the name stays.

## Decision

The naming rule decides (owner's request in the trial 13 task: "A naming rule for a field takes precedence over
StyleBro's defaults: write its style, or leave the field alone when StyleBro can't write that style"). Rules are
ordered like Roslyn's EditorConfigNamingStyleParser (priority, modifiers, accessibilities, symbol kinds, name), a rule
without a severity doesn't exist (as in Roslyn), and a rule turned off (none/silent) leaves StyleCop's default. Private
fields keep their existing handling (`stylebro_private_*` keys first).
