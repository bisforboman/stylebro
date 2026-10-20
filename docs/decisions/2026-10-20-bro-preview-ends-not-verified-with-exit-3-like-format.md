# bro preview ends "Not verified" with exit 3, like format (2026-10-20)

## Question

Trial 36, SharpZipLib and NodaTime: `bro preview init` said "clean after 1 run" and exited with 0, while the same steps in
`bro format` ended "Not checked: IDE0049 ..." and "Not verified" with exit code 3: the preview runs format one run at a
time and wrote the gap lines only to its log. What should the preview say then?

## Choices

1. **Like format, exit code 3** (owner's pick): the last run's "Not checked:" lines and the "Not verified" line, "nothing
   left to change after 1 run, but not verified (below)" in the summary, `--json` `clean` false with `notChecked`.
2. Say it, but exit with 0 (it only previews).

## Decision

Like format, with exit code 3.
