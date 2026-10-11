# Migrate pins StyleCop casing (2026-10-06)

## Question

Nerdbank.Streams has a `dotnet_naming_rule` asking for camel case on static fields (at `suggestion`) and IDE1006 off
("StyleCop handles these for us"). After `stylebro-migrate --write`, BRO1306 followed that naming rule and renamed 178
fields StyleCop was happy with. Should migrate follow the repository's naming rules or StyleCop's casing?

## Choices

1. Pin StyleCop's casing: `--write` writes explicit `stylebro_*` keys, which win over `dotnet_naming_rule`; a new
   project without migrate keeps following its naming rules.
2. Follow the naming rules (what happened), and document how to opt out.
3. Follow a naming rule only when IDE1006 is on.

## Decision

The owner: choice 1. Before:

```csharp
private static readonly Version ProtocolVersion = new(1, 0);   // StyleCop-clean
private static readonly Version protocolVersion = new(1, 0);   // after migrate + dotnet format
```

After: unchanged. Agent's choices: private instance fields already had a key (`stylebro_private_field_naming`, written
by migrate); private constants and `static readonly` fields had none, so `stylebro_private_static_field_naming`
(`PascalCase`, `camelCase`, `_camelCase`, for both) was added, documented on [BRO1306](../rules/BRO1306.md); migrate
writes `PascalCase`. Nerdbank.Streams after the fix: 0 renames, 0 changes from `stylebro-migrate format`.
