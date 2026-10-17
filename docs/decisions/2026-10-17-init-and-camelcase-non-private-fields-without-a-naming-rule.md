# init and camelCase non-private fields without a naming rule (2026-10-17)

## Question

Trial 29 (Harmony): the code names its public and internal fields in camelCase (public 411 to 21, internal 59 to 0) and has
no naming rule. `init` counted non-private fields only against an enforced naming rule (2026-10-15), so BRO1306 stayed on
with StyleCop's PascalCase and renamed the internal fields in 22 files (40 more were held back only by the string guards).
What should `init` do?

## Choices

1. **Count it, BRO1306 off**: count the casing of public and internal fields like the other conventions; when the code
   doesn't follow PascalCase, BRO1306 is turned off with an `init:` line. BRO1306 then stops checking constants and
   static readonly fields too.
2. Count it, and add a key (`stylebro_non_private_field_naming`) BRO1306 follows for non-private instance fields.
3. Leave it: StyleCop's default stays, the user turns BRO1306 off.

## Decision

**Choice 1: Count it, BRO1306 off** (owner's choice, 2026-10-17). `Conventions.CountNonPrivateNaming` counts, for fields
no enforced naming rule covers, the public, internal and protected internal fields that aren't constants or static
readonly (those have their own convention): PascalCase as BRO1306 wants, or not. A name BRO1307/BRO1308 would change
(`m_x`, `a_b`) doesn't count. It shares the `dotnet_diagnostic.BRO1306.severity` convention with the naming-rule count.
