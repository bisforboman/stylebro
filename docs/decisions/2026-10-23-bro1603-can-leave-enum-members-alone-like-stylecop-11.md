# BRO1603 can leave enum members alone like StyleCop 1.1 (2026-10-23)

## Question

Trial 48, benjamin-hodgson/Pidgin (StyleCop 1.1.118, migrated): BRO1603 added a period to 5 enum-member summaries in 2
StyleCop-clean files. Probed: StyleCop 1.1.118's SA1629 doesn't check enum members, 1.2.0-beta.556 does, and BRO1603 follows
1.2. Migrate already adjusts other rules for 1.1.x repositories (SA1141/SA1142/SA1316/SA1414 off, BRO1401's switch
expressions). What should migrate do here?

## Choices

1. **An option migrate sets** (owner's pick): a BRO1603 option to leave enum members alone (default: checked, like 1.2);
   migrate writes it for StyleCop 1.1.x repositories, so a StyleCop-clean 1.1.x repository stays unchanged.
2. Document it: keep 1.2's behaviour and list the 1.1.x difference; migrated 1.1.x repositories get the periods.

## Decision

An option migrate sets: `stylebro_punctuation_check_enum_members` (`DocumentationPeriods.EnumMembersKey`, default true);
`Migration` writes `false` when the StyleCop version is 1.1.x.
