# Trial 60: not clean, the host soak is clean (2026-10-29)

## Question

Trial 60 (apache/logging-log4net, Cysharp/MasterMemory, dotnetcore/EasyCaching random, seed 60; khellang/Middleware targeted
at trial 59's shadowed-props fix) found these, all fixed in its PR:

- EasyCaching: BRO1513 and BRO1505 around a `#region` right below a method's `}` gave a blank line above the region or not
  depending on which fix ran first: the two engines wrote different trees, each clean in its own check.
- log4net: BRO1120 removed the first and last empty `//` lines of the Apache license header (inside `#region Apache
  License`) in 325 of 402 files (owner's decision: the file header is left alone).
- Middleware cloned without its submodule: ProblemDetails compiles files from `lib/AspNetCore`, didn't compile, and format
  and check said clean without a word (owner's decision: format names the submodule and leaves out IDE0005).

Held: trial 59's message named both shadowing props files, init pinned the preset in each; #312's fast check matched the
full check (record deleted) and `check --build` on all four; every TWAE build and test suite matched the untouched code in
round 2. Engine soak: round 1 differed on EasyCaching's tree (the fix-order bug); round 2 identical on all four apart from
the load-warning note and the old engine's duplicate finding lines (allowed today); no fallback. Is the trial clean, and is
the soak clean?

## Choices

- Trial: **Not clean** (tally stays 0) / Clean (tally 1).
- Soak: **Clean soak** / Not clean.

## Decision

Trial: "Not clean" (owner, 2026-10-29): the tally stays 0. Soak: "Clean soak": the fifth clean soak with the host as the
default engine.
