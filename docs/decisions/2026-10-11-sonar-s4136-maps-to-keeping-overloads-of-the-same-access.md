# Sonar S4136 maps to keeping overloads of the same access together (2026-10-11)

## Question

With Sonar's S4136 (method overloads grouped together) on, the migration and `init` turned on
`stylebro_keep_overloads_together = true`, which keeps every overload group together, also overloads of different access
that StyleCop's SA1202 order (and BRO1001's default) puts apart. S4136 itself only asks for overloads with the same access
to be adjacent. What should the Sonar mapping write?

## Choices

1. Keep `true`: every overload group stays together.
2. **Add a 'same access' mode**: `stylebro_keep_overloads_together = same_access` keeps only overloads with equal access
   together, exactly like S4136, and the Sonar mapping writes it.
3. Drop the mapping and leave S4136 to Sonar.

## Answer

**Choice 2** (owner: "Add a 'same access' mode"). The rules side adds the `same_access` value to BRO1001; the Sonar
mapping (`src/StyleBro.Migrate/data/sonar-mapping.tsv`) writes it for S4136. `init`'s own detection of overloads kept
together still writes `true` (it counts groups BRO1001 would split, whatever their access).
