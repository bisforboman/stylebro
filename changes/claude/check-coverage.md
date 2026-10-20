### Changed

- **`bro format` and `bro check` no longer say "Clean" when part of the build went unchecked.** A project or target
  framework `dotnet format` couldn't load, a project that needs a missing workload, a project the solution doesn't list,
  configurations past the four it runs, files a pass leaves out, and rules whose fixes can't run (xunit.analyzers 1.24+,
  held-back IDE fixes) each get a `Not checked: <what>: <why>` line, and the command ends with `Not verified: ...` and
  exit code 3. `--json` lists them as `notChecked`. A project that references an F# project counts too now (it was a
  warning before), and a project no run loaded exits with 3 instead of 1.

### New

- **`bro check --build`** builds the solution in every configuration (and platform) with warnings as errors after the
  check: a finding of `bro format`'s rules fails it, any other error only when a copy of git HEAD builds without it.
