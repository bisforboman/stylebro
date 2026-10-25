# protobuf-net/protobuf-net.Grpc (trial 55, targeted at trial 54's fixes)

main 3471179c, 164 .cs, protobuf-net.Grpc.slnx with C#, F# and Visual Basic examples, a `VS` solution configuration,
libraries net472/netstandard2.0/2.1/net8.0/net10.0, AspNetCore libraries net8.0;net10.0, tests net472;net8.0;net10.0 that
reference the AspNetCore library and the test proxy (no net472) and ClientFactory (no net472, no net10.0): the lens of
trial 54's Dapper fix (`SelectFrameworkProps`), with Nerdbank.GitVersioning (full clone) and PublicApiAnalyzers bringing
props and targets into every project. Central package management, nuget.config `<clear />` (a `<clear/>` in a comment
above it: the trial's prep script first put the feed inside the comment; fixed in prep, not a StyleBro issue), global.json
10.0.203 (lowered), a WPF example (`EnableWindowsTargeting=true`), no StyleCop (init).

## Baseline (untouched)

Build green (10 RS0016 warnings). TWAE Debug/Release fail: 20 RS0016 (public API not declared) errors, the rest of the
graph not compiled. Tests (Debug, Release): 14 + 184 + 184/185 + 216 pass per framework (net472 doesn't run on Linux);
the DuplexEcho integration tests are timing-flaky: the smoke run of the untouched code failed 3 + 2 of them, the trial's
baseline none, the after-fix runs 5 (host) and 3 (dotnet-format) different ones.

## Default engine (host)

- preview init 275 s: 103 files, clean after 1 run, 2 kept findings (BRO1311, tuple names used in disabled `#if` code).
  init turned 18 rules off for mixed code and 2 as not followed: a mixed code base, honest.
- format 235 s: 17 runs per pass (6 frameworks, Release, shared-file groups), run 2 clean, exit 0. **The net472 runs
  loaded every project** (no "partly loaded" or "Not checked" lines besides the IDE0049/xUnit notes), on both engines:
  trial 54's `SelectFrameworkProps` holds here.
- Visual Basic projects left alone and said so; xunit.analyzers 1.27.0 (no Fix All) a note.
- **Finding 3 (fixed):** "src/protobuf-net.Grpc.Reflection/Generated/Reflection.cs is generated and checked in: rebuild
  and commit it." Nothing in the build writes it (protogen output from a grpc .proto, run by hand); `IsGenerated` took
  "a build file names it" from `protobuf-net.Grpc.Reflection.csproj` in a ProjectReference (a substring match). The name
  must now be whole (no word character, '.' or '-' on either side).
- check 185 s exit 0; check --build 385 s exit 3, "Not verified: the Debug ..., VS ... builds didn't compile the projects
  named: a project they need failed, as in the untouched code's build": the 20 RS0016 errors, honest.
- TWAE: the same 20 RS0016 errors as the untouched code (Debug and Release).
