# How few places `init` follows (2026-10-10)

## Question

With fewer than 10 places, `init` followed them whenever they all agreed, even a single one: the package check's test
project had one `""`, so `init` wrote `stylebro_empty_string_style = literal` and BRO1106 turned every `string.Empty` into
`""`. How many places should `init` need before it follows the code?

## Choices

1. One place is enough when nothing disagrees (as built).
2. A minimum of 3 places; below that StyleBro's default stays and nothing is written.
3. Too few places turn the rule off, like mixed ones.

## Decision

The owner: a minimum of 3 (choice 2). Fewer than 3 places (both forms together): `init` reports "too few places to
tell, <key> stays <default>" and writes nothing (no setting, no rule turned off), unless the key is decided elsewhere (the
repository's `.editorconfig`, a Sonar setup, a naming rule), which is reported as before. 3 to 9 places: followed when
all agree, else the rule is turned off; 10 or more: 75 % as before. RealWorld's 2 `_` fields therefore keep the default
and are renamed (accepted). At the same time `init`'s output got a verdict first on each line (`kept`, `default`, `off`,
`mixed`, `too few`, `set`) and a summary of what was kept and turned off, and the written block a `# init:` comment above
each line saying why (the counts) and how to choose a form later.
