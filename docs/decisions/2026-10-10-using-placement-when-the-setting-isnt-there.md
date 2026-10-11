# Using placement when the setting isn't there (2026-10-10)

## Question

BRO1008 followed StyleCop's default (inside the namespace) when `csharp_using_directive_placement` wasn't set. Repos
without the preset and the setting then got their usings moved: the real-world runs (no preset) moved them into the
namespace in 257 Newtonsoft.Json files and 199 eShopOnWeb files, repos that clearly put them outside. What should
"not set" mean?

## Choices

1. Preserve: nothing is reported until a setting says where usings go.
2. StyleCop's default: inside.
3. The SDK's default: outside.

## Decision

1 (owner). BRO1008 acts only when a setting says where usings go: the preset says `outside_namespace`, and
`stylebro-migrate` writes StyleCop's choice from `stylecop.json` (`inside_namespace` when it isn't set, StyleCop's default),
so migrated repositories keep SA1200's behavior.
