# Performance notes

Build-time measurements, the benchmark and the performance fixes. More performance history is in history.md ("Added ..." sections). Moved from CLAUDE.md on 2026-10-23.

## Performance (2026-10-02)

- Build wall-clock with vs without StyleBro (all rules + preset, `--no-incremental`, 2 runs each): private app no
  measurable difference (9-12 s either way); Newtonsoft.Json 8.4 -> ~11.8 s. Build timings and `ReportAnalyzer`
  numbers swing ~2x between runs on this machine, so they can't compare versions.
- `scripts/benchmark` (README there): one compilation of a source folder, all analyzers together, single-threaded,
  the compiler's per-analyzer telemetry (same as ReportAnalyzer), median after a warm-up run. On Newtonsoft.Json's main
  project: alpha.8 1,096 ms (FieldNamingAnalyzer 468); StyleCop 1.2 1,671 ms for its 182 analyzers.
- Fixed: FieldNamingAnalyzer's `CanRename` walked every token of the whole type, built a Regex and recomputed every
  sibling field's new name PER FIELD (quadratic in fields per type). Now `FieldNames.TypeFacts` (words in strings and
  disabled text, inferred member names restricted to the type's field names, new-name counts per style) is built once
  per type per compilation (cache in a compilation-start action). Same results (218 findings both ways); 468 -> ~150 ms,
  StyleBro total ~800 ms. Next most expensive: DocumentationAnalyzer (~160 ms), CommentTextAnalyzer (~80 ms).

## Performance audit fixes (2026-10-22, owner's decisions)

- BRO1112's and BRO1409's fixes sort with BRO1001 on the document they just changed (`GetSortChangesAsync(changed, namesFrom:
  document.Project.Solution, ...)`): the names in code are cached per Solution, and asking with the changed document's solution
  scanned the whole solution once per fixed file (Newtonsoft.Json: BRO1112's Fix All 387 s -> ~15 s). `CamelCaseRenamer.NameScans`
  (AsyncLocal counter, tests only) lets `LinkedFileFixAllTests.FixAllThatSorts_ScansTheSolutionOnce` assert one scan per Fix All.
  A new fix that reads solution-wide data must ask with the solution it started from, never a forked document's.
- TrailingBlankLines calls `IsStandalone` last (it can only be true with a blank line below the comment).
- Owner ("Note it, don't fail"): a held-back SDK rule (`HeldBackGap`, IDE0049 on SDK 10.0.1xx) is still a `Not checked:` line and
  in `notChecked`, but `Unverified(gaps)` leaves it out of the verdict and the exit code (format, check, preview).
- Fix-time limits fail the real-world jobs (2026-10-24, owner's choice, docs/decisions/2026-10-24-real-world-fix-time-limits-fail-the-job.md):
  `MaxFixSeconds` in scripts/realworld/repos.psd1 is about 2x the median of 13 runs per repo on its own runner after #280
  (runners vary about 2x between runs; the slowest was at most 1.27x the median). Over it is a problem (`::error::`, the
  job fails) naming the repo, the time and the limit; `FixTimeWarnOnly = $true` makes it a warning for one repo. The job
  doesn't log per-id costs: `scripts/benchmark/Measure-FixLoop.ps1` finds the slowest ids. Newtonsoft's BroFormat step
  has `MaxBroFormatSeconds = 3600`, warning only (2 timed runs: 1162 s, 2401 s) until nightlies show its median.
