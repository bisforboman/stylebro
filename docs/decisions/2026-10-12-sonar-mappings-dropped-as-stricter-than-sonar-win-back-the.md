# Sonar mappings dropped as stricter than Sonar: win back the StyleBro ones (2026-10-12)

## Question

PR #150 enforced the policy that a Sonar mapping never reports what the Sonar rule itself accepts, and dropped 17 mappings
whose fixing rule reported more (S121, S818, S1066, S1116, S3878 and S8969 fixed by StyleBro rules; the rest by .NET SDK
rules). Should the dropped mappings come back, and how?

## Choices

1. **Win back the StyleBro ones**: give each StyleBro rule a setting that makes it report exactly what its Sonar rule
   reports (or less), map the Sonar rule to the rule plus that setting again, and leave the SDK ones not covered.
2. Leave all 17 not covered.
3. Map them again as they were, accepting fixes Sonar doesn't ask for.

## Decision

**Choice 1** (owner: "Win back the StyleBro ones"). Each setting was checked against SonarAnalyzer.CSharp 10.35.0.4138 on
probe cases covering the differences (with the setting, StyleBro reports nothing Sonar doesn't):

| Sonar | Rule and setting |
|---|---|
| S121 | BRO1514 with `csharp_prefer_braces = true` and `stylebro_braces_statements = if_and_loops`: not `using`, `lock`, `fixed` or a deconstructing `foreach` (S121 registers if, else, for, foreach, while and do only) |
| S818 | BRO1135 with `stylebro_upper_case_literal_suffixes = lone_l`: only a suffix that is just `l` (S818 skips an `l` after a `u`, and a suffix ending in `u`) |
| S1066 | BRO1149 with `stylebro_allow_nested_dynamic_ifs = true`: no merge when a condition has a part of type `dynamic` (S1066 skips the inner condition's; a setting, not always on, because asking for the types costs ~20 ms on Newtonsoft.Json, over the benchmark's limit) |
| S1116 | BRO1101 with `stylebro_allow_semicolon_after_closing_brace = true` (S1116 reports empty statements only) |
| S3878 | BRO1151 with `stylebro_params_arrays = except_object_and_jagged`: not for `params object[]`, `params Array[]` or jagged arrays |
| S8969 | BRO1147 with `stylebro_allow_null_forgiving_on_unannotated = true`: a `!` on a property, field or method return without nullable annotations (or a `var` local initialized from one) stays, Sonar's "oblivious" check |

A narrowing `stylebro_` setting isn't written when the StyleCop setup turns the same rule on (SA1106 for BRO1101, SA1503
for BRO1514): StyleCop's wider scope stays, and the report notes it. S100 and S101 (BRO1309) stay not covered: BRO1309's
differences (events, enum members, enums, delegates, `_` inside names, test classes) are wider than one setting.
