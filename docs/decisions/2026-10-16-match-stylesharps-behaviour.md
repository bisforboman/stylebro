# Match StyleSharp's behaviour (2026-10-16)

## Question

Trial 24's Refit uses StyleSharp.Analyzers 5.1.2 (634 SST rules at error). `stylebro-migrate` reads StyleSharp like
StyleCop ([2026-10-14](2026-10-14-read-stylesharp-like-stylecop.md): SST rule = the StyleCop rule at its number), but
several SST rules mean something else: SST1623/SST1624 count `protected set` as restricted and accept "Gets or sets" on
`{ private get; set; }`, SST1201 orders nested records after classes, SST1305 allows `id`/`ok` and 11 more words,
SST1413 doesn't check switch expressions, SST1501/SST1502 accept empty `{ }` on one line. So StyleBro's fixes changed
StyleSharp-clean code, and some of them broke StyleSharp's own build (42 SST1624 errors) while both packages ran. What
should migrate do where StyleSharp and StyleCop differ?

## Choices

1. **Match StyleSharp's behaviour** (owner's pick): where a StyleBro rule can be configured to do what the SST rule does,
   add the setting (an additive option, default unchanged) and have migrate write it for StyleSharp repositories;
   differences that can't be configured are documented in docs/migrating.md.
2. Keep reading StyleSharp as StyleCop and document the differences only.
3. Turn the StyleBro rules whose SST counterpart differs off for StyleSharp repositories.

## Decision

**Choice 1** (owner: "Match StyleSharp's behaviour"). New settings: `stylebro_summary_wording = stylesharp`
(BRO1604/BRO1605), a `record` kind and `a|b` groups in `stylebro_member_order` (BRO1001), and
`stylebro_trailing_comma_switch_expressions` (BRO1401; also written for StyleCop 1.1.x instead of turning BRO1401 off).
Migrate writes StyleSharp's Hungarian word list (BRO1310) and `stylebro_allow_empty_single_line_blocks = true`
(BRO1508/BRO1509). SST1108 (commented-out code files) no longer maps to SA1108. The mapping was re-audited against
StyleSharp 5.1.2 with `Compare-WithStyleCop.ps1 -StyleSharp`.
