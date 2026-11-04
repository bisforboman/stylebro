# bro init carries IDE0040 suppressions over to BRO1404 (2026-11-04)

## Question

Trial 67, RestEase: `bro init` sets IDE0040 to `none` because BRO1404 replaces it. The code has
`#pragma warning disable IDE0040` around an implicit-private nested interface that a source-position test relies on
(`SanityCheckTests.ThrowsIfInterfaceIsImplicitPrivate` expects the diagnostic at column 19). BRO1404 does not read that pragma
and added `private`, so the diagnostic moved to column 27 and the test failed after StyleBro. `bro migrate --write` carries SAxxxx
suppressions over to the replacing rules; `bro init --write` did nothing for IDE ids it turns off.

## Choices

1. `init --write` carries IDE0040 suppressions over to BRO1404, like migrate does for SA ids (recommended).
2. BRO1404 itself honors IDE0040 suppressions (analyzer and fix scan trivia and attributes).
3. Leave it and document the limit.

## Decision

Owner (2026-11-04, AskUserQuestion): choice 1. `bro init --write` adds BRO1404 to `#pragma warning disable/restore IDE0040`,
`[SuppressMessage]` and `<NoWarn>` entries, the same way migrate does for the SA ids. Pragmas written after init are not covered.
