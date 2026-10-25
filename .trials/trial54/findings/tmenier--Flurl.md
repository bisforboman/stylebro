# Trial 54: tmenier/Flurl (random, seed 54)

dev 36a3fa2b, 81 .cs, libraries net6.0;netstandard2.0;net461 (and net472), tests net6.0;net48;net461, a code generator
(Flurl.CodeGen writes GeneratedExtensions.cs and UrlBuilderExtensions.cs from doc strings), K&R braces with namespace braces on
their own line, tabs, `.editorconfig` with no charset/final-newline keys (61 of 81 files with a BOM), no analyzers.
`NuGet.Config` `<clear/>`s (trial feed added). Path A (init). StyleBro main dc107c4, 0.8.0-trial54.1. Tests: net6.0 only
(net461/net48 need Mono on Linux).

Setup: the reference in each of the 5 csproj files (no props).

## Results (default engine; the host engine identical but for PR #287's load note)

| Step | Exit | Notes |
|---|---|---|
| base tests Debug/Release | 1 | 521/547: the 26 RealHttpTests need the network (httpbin.org) |
| `bro preview init` | 0 | 50 files, 394 changes, clean after 1 run, 146 kept findings, baselined |
| init --write | 0 | 47 settings kept, 10 rules off; namespace braces note: IDE0055 off until the team decides (decision 2026-10-16) |
| `bro format` | 0 | runs for net461, net6.0, netstandard2.0 (net472/net48 compile the same), 50 files, no whitespace pass (IDE0055 off), run 2 clean |
| `bro check`, `check --build` | 0 | Clean; Debug and Release: no findings, no new errors |
| TWAE Debug/Release | 0 | |
| tests Debug/Release | 1 | the same 26 network failures, same names |

Kept (146): 139 BRO1603 in the generated files and in Url.cs/ISettingsContainer.cs, whose doc texts the generator also holds
as strings (trial 44's doc-text-at-runtime guard: conservative, a hand fix in a generated file would be overwritten anyway),
7 BRO1309 in RealHttpTests/PostTests (anonymous-object property names read as JSON by httpbin). All listed after the run (146
lines; the decision of 2026-10-10 lists every one) and baselined. No BOM/final-newline changes.

## Findings

None.
