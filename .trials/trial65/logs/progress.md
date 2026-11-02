# Trial 65 progress
- 20:16 round 1 (base + default, host only; owner dropped the soak) started: lane A SC, AR, DE; lane B FC.
- 21:00-21:55 round 1 (host only) done on 0.8.0-trial65.1. Findings so far:
  1. AR: `bro check --build` leaves restore output (Release last) that makes later full `check` fail to load core+Redis ("CoreCompile does not exist"), because the Demo project (and Redis) switch ProjectReference (Debug) / PackageReference of the same names (Release). Fixed: ForgetRestore deletes project.nuget.cache after builds in 2+ configurations (verified on the AR clone). Fixture shape still to reproduce (net6.0 matters, same-named packages matter; Redis project switches too).
  2. FC (CSharpier): tools/Bench/.editorconfig sets IDE0055 = silent; whitespace pass runs there (only Suppress counts as off) and CSharpier undoes it: "Not clean: CSharpier puts back ...". With `none` the findings vanish.
  3. FC: 9 findings in Release-only code (in-solution generator => format cannot load Release), TWAE Release build fails after StyleBro (baseline passed): documented kept-findings class.
  4. FC: CSharpier would change Directory.Build.props (my 2-space insert into 4-space file) and untouched RecordView.cs.
  5. DE: InterperterCreation (stopwatch < 200ms) failed after run: timing flake while the FC lane ran; to re-run alone.
  6. DE has two solutions: init needs --project (by design).
  7. FC baseline tests: 578 failures in Reading (Debug) + 1+1 elsewhere, same set after.
- Fixture for finding 1 (config-dependent references): tried ~8 variants of a small repo (net6.0, TargetFrameworks plural, same names and versions, both projects switching, real csproj files copied in); none failed before the fix, only the real AspNetCoreRateLimit clone does (cause outside the csproj files: not found). Dropped the shape and the 'check-again' runner step; covered by a unit test + mutation entry + the real repo (round 2).
- Owner (AskUserQuestion): IDE0055 silent with CSharpier = off. Implemented in WhitespaceExclusions + unit test + mutation entry.
