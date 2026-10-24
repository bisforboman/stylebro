# drewnoakes/figgle (trial 52, random entry 52)

Commit 804efc8e65276154ff7d71e2062ecd475ccd4e9f. StyleBro main 8247120, packed as 0.8.0-trial52.1.
Setup: path A, step 1 in `src/Directory.Build.props` and `tests/Directory.Build.props` (none at the root; site/ and
samples/ have no props and stay without StyleBro), version in the root Directory.Packages.props, then `bro init --write`.

## Baseline (untouched)

Debug build green; TWAE Debug/Release green; tests Figgle.Tests 323, Figgle.Generator.Tests 30,
Figgle.Generator.AcceptanceTests 4 (net9.0), Debug and Release.

## Both engines

preview init 0 (14 files, clean after 1 run; it added the reference to src/ and tests/ props AND a new root
Directory.Build.props for site/ and samples/, which have none: right, they import the root one), init --write 0, format 0
(14 files, run 2 clean; "Without StyleBro: samples/..., site/..." said honestly, "Shared files: Figgle | Figgle.Tests"
split into 2 parts, generators built first), check 0 Clean (1 s: format's clean record stands), check --build 0 Clean,
TWAE Debug/Release green, tests 323 + 30 + 4 x 2. Host and default: identical trees, exit codes and --json (path aside).

No findings.
