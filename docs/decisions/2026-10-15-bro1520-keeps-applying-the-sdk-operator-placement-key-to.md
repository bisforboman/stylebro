# BRO1520 keeps applying the SDK operator-placement key to ?/: (2026-10-15)

## Question

The SDK documents `dotnet_style_operator_placement_when_wrapping` for binary operators. BRO1520 applies it to the
conditional operator's `?` and `:` as well (documented on the rule page in PR #208). Keep that, or give `?`/`:` a key of
their own (like Roslynator's `roslynator_conditional_operator_new_line`)?

## Choices

1. **Keep applying the SDK key to `?`/`:`**: one key decides where every operator of a wrapped expression goes.
2. A separate `stylebro_*` key for the conditional operator.

## Decision

**Choice 1** (owner's choice 2026-10-15). No code change; the rule page links this decision.
