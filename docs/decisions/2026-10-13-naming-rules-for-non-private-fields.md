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

## Extended (2026-10-13): only enforced naming rules count

A repository whose naming rules nothing enforces could get its non-private fields renamed to match them. The owner's
policy, as for other settings ("a key nothing enforces doesn't win"): a naming rule counts for non-private fields only
when it is enforced, i.e. IDE1006's severity (when set), or else the rule's own severity, is suggestion, warning or error
(a rule at `none` never counts). Otherwise StyleCop's default applies (PascalCase for public and internal fields,
camelCase for protected ones).

Private fields: the analyzer keeps following the rule (its fallback, camelCase, is StyleBro's own default, not
StyleCop's), and `stylebro-migrate init` no longer lets a rule decide the private field style or the casing of private
constants/static readonly fields while IDE1006 is off: the code decides and the key init writes wins over the rule.

No new key for "follow this unenforced rule": raising the rule's `severity` to `suggestion` already says it, and init
counting non-private fields against the rules would be a fourth naming convention for a rare case (YamlDotNet, the
trial that prompted #167, sets its rules to `suggestion`, so they count).
