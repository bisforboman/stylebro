# Trial 66 not clean and a kept raw string file is Not verified (2026-11-03)

## Question

Trial 66 (jwt, moq4, SecurityHeaders, FluentBuilder; PR #325) found two StyleBro bugs and fixed them: a single entry in
`<TargetFrameworks>` gave a false "Not checked ... CoreCompile does not exist", and moq4's formatting rewrote the text of a
`$$"""` literal in every run and never settled. The guard added for the second keeps the file as it was, and Roslyn's own IDE0055
keeps reporting three spots there, so moq4's warnings-as-errors build fails while `bro format` and `bro check` say Clean.
Is the trial clean, and what should format and check say about a kept file?

## Choices

- Verdict: clean (tally 1, an Opus session re-checks the same repositories first), or not clean (tally 0).
- A kept file: Clean with a `Kept ...` line (as now), or Not verified (exit 3, a `Not checked:` line naming the file).

## Decision

Owner (2026-11-03): "Not clean" (tally 0 after trial 66), and a file kept by the raw string guard makes format and check say
Not verified, with a `Not checked: <file>: a fix would change the text of a raw string literal` line, so they agree with the
build. Not done in PR #325: backlog `scripts/backlog/work/078-kept-raw-string-files-are-not-verified.json`.
