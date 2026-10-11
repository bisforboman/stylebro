# Findings kept on purpose, and a "clean" check for agents (2026-10-10)

## Question

Some findings have no fix by design: a rename the fix declines because it would break something only the fix can see
(the name in a string or a `nameof` anywhere in the solution, a member that would hide it, a use in generated code, a
tuple rename that adds errors). An AI agent (or a person) sees "BRO1303: Rename '_count' to 'count'", finds that
`dotnet format` doesn't fix it, and renames by hand, breaking what the check protects. And `dotnet format
--verify-no-changes` fails while such findings exist, so a loop "until clean" never gets there. How should StyleBro
make these findings look different and give a reliable clean check?

## Choices

1. Leave it to the docs: the rule pages explain "Reported but not fixed".
2. Make the reasons discoverable: the fix offers no code action for a kept finding, the fixes record each kept finding
   with its reason, `stylebro-migrate format` lists them, and its `--verify-no-changes` treats "only kept findings
   left" as clean (exit 0), failing only when formatting would change a file.
3. Report kept findings at a lower severity: the analyzer can't know the fix-side reasons (other projects), so it
   can't pick the severity.

## Decision

2 (owner's request). Where the analyzer can see a reason it already doesn't report (design rule 3). For the fix-side
reasons, the renamers expose why they keep a finding (`CamelCaseRenamer.GetKeptReasonAsync`,
`TupleElementRenamer.GetKeptReasonAsync`, BRO1409's `GetKeptReasonAsync`, one `KeptReason` enum): the code fix registers
no action then, and when `STYLEBRO_KEPT_FINDINGS` names a file the fix appends the finding and its reason there.
`stylebro-migrate format` sets it and lists them after its last run. `dotnet format --verify-no-changes` doesn't run
fixes, so `stylebro-migrate format --verify-no-changes`, when that reports findings, formats a temporary copy and
decides by whether any file changed. Exit codes: 0 clean, 2 something left to change (also a plain run still changing
after three runs, which used to exit 0). The diagnostic messages stay short (they show on every finding; the help link
leads to the rule page); the descriptions and the rule pages say what a kept finding means. Follow-ups for agents
(`--json`, per-file format, an AGENTS.md section from `init`, llms.txt) are in the backlog.
