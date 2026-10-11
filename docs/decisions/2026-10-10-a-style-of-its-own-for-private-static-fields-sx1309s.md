# A style of its own for private static fields (SX1309S, 2026-10-10)

## Question

StyleCop's SX1309S wants private static fields that aren't `readonly` or `const` to begin with `_` (no options; fields in
`NativeMethods` classes skipped). On its own, with instance fields named `count`, BRO1303 couldn't express it: its one
style covered instance and static fields alike. How should that be configured?

```csharp
private int count;                 // stays
private static int instances;      // SX1309S: _instances
```

## Choices

1. A new key `stylebro_private_mutable_static_field_naming = camelCase | _camelCase`. Not set: a `dotnet_naming_rule` for
   private fields that requires just `static` with a camel-case style decides (like the existing rules for constants
   and `static readonly` fields), else `stylebro_private_field_naming`'s style, so nothing changes without it.
2. Reuse `stylebro_private_static_field_naming`: it already means private constants and `static readonly` fields
   (BRO1306, pinned to `PascalCase` by `stylebro-migrate`), so it would need splitting and migrated repositories change.
3. A second value in `stylebro_private_field_naming` (`camelCase, static: _camelCase`): one key, but a format no other
   key uses.
4. Leave it as is (one style for both).

## Answer (agent's proposal, for the owner to review)

Choice 1. Every field still gets one complete new name from `FieldNames.GetRename` (prefix, underscores and casing in one
rename, BRO1307/BRO1308/BRO1310 included). `stylebro-migrate` pins the key to the instance style, like the static style
(2026-10-06), so a naming rule StyleCop never enforced doesn't rename fields. SX1309S alone stays "not expressible" in
its report: StyleCop's SA1309 (no `_`) checks every field, so a repository with SX1309S has it off and accepts
instance fields with and without `_`, which BRO1303 in either style would unify. The key is for teams configuring
StyleBro directly (`_` on static fields only).
