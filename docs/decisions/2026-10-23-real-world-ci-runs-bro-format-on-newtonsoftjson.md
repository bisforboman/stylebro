# Real-world CI runs bro format on Newtonsoft.Json (2026-10-23)

## Question

`bro format` on Newtonsoft.Json didn't settle (`#endregion` lines its net20/net35 runs indented their own way) while the
real-world job passed: that job runs plain `dotnet format analyzers` with StyleBro's ids, without the whitespace pass or
the per-framework runs. `bro format` takes ~13-20 minutes per engine there. Should the real-world job also run
`bro init --write` + `bro format` and fail on "Not clean"?

## Choices

1. **Newtonsoft only, default engine** (owner's pick): one extra `bro format` step on the multi-target reference
   repository, failing on exit 2.
2. Newtonsoft, both engines (`--engine host` too).
3. Every repository, default engine.
4. Not now: notes only.

## Decision

Newtonsoft only, default engine.

Applied as: `BroFormat = $true` on Newtonsoft in `scripts/realworld/repos.psd1`; `Invoke-RealWorld.ps1` resets the clone
after its checks, runs `bro init --write` and `bro format` with the package hook, and adds a problem for an exit code
other than 0 or 3.
