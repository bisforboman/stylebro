### Changed

- **`bro migrate`** carries over the rules StyleCop.Analyzers 1.3 added: where SA1143 is on, it turns on BRO1524 (conditional expression layout); SA1315 turns on BRO1313 (parameter names of overrides and implementations); SA1654 turns on BRO1617 (generic crefs in braces). SX1412 (UTF-8 without a byte order mark) becomes `charset = utf-8`. Before, the report listed these rules as not covered.

### Fixed

- **`bro migrate`** reads the early StyleCop.Analyzers 1.3 betas right: SA1143, SA1315 and SX1412 count as off before 1.3.98-beta, SA1654 before 1.3.109-beta, SA1317 and SA1653 before 1.3.79-beta (those versions don't have them), so code StyleCop never checked for them isn't changed.
