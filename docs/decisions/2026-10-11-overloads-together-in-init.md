# Overloads together in `init` (2026-10-11)

## Question

BRO1001 sorts by kind and access, so a public and a private overload of one method end up apart (Scrutor's
`CanDecorate`). `stylebro_keep_overloads_together = true` keeps them together. Should `init` set it?

## Choices

1. **Detect**: count overload groups whose members BRO1001 would sort to different places (different sort keys; only
   those can be split) as next to each other or apart, and write `true` when the code keeps them together.
2. Always `true` in the preset.
3. Leave it to Sonar's S4136 and the user.

## Answer

**Choice 1** (owner: "Detect"). The usual thresholds over those groups; mixed or apart keeps the default (`false`), nothing
is turned off. Scrutor has one such group, too few to tell, so its default stays: the owner's rule needs 3 places.
