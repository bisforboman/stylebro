# Overloads together with the same access only (2026-10-11)

## Question

`stylebro_keep_overloads_together = true` keeps every method with the same name together. Sonar's S4136, which
`stylebro-migrate` maps to it, groups overloads only when they also share accessibility, `static` and `abstract`: Sonar
10.31 doesn't flag `public Dispose()` and `private Dispose(bool)` apart. Mapping S4136 to `true` makes BRO1001 move
code that Sonar accepts. How should StyleBro express S4136?

## Choices

1. **Add a `same_access` mode**: `stylebro_keep_overloads_together = same_access` groups overloads exactly like S4136
   (name, accessibility, static, abstract); `true` and `false` stay as they are.
2. Change `true` to Sonar's meaning.
3. Keep mapping S4136 to `true` and document the difference.

## Decision

**Choice 1** (owner: "Add a 'same access' mode"). `MemberOrderOptions.OverloadsSameAccessOnly`; BRO1001's overload
anchors add the access, static and abstract to the group key in that mode. Documented on BRO1001's page and in
configuration.md; `stylebro-migrate` maps S4136 to `same_access` (the migrate side's change).
