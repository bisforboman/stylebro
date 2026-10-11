# `init` and a repository key the code contradicts (2026-10-10)

## Question

NCronJob and linkdotnet/Blog set `dotnet_style_operator_placement_when_wrapping = end_of_line` in their .editorconfig, but
85 of 87 (and 41 of 42) wrapped operators start the line: no SDK rule enforces that key, so it was never applied. `init`
reported "set in .editorconfig", and BRO1520 then moved 82 and 40 lines. What should `init` do when the code clearly
contradicts a key the repository sets?

## Choices

1. Turn the StyleBro rule that enforces the key off, leave the key alone, and explain it in the report and an `# init:`
   comment.
2. Keep the key and let the rule rewrite the code (what 0.5.0-alpha.1 did).
3. Overwrite the key with what the code does.

## Answer

**Choice 1** (owner: "Turn the rule off, explain"). It applies to every convention whose StyleBro rule reads an SDK key
the repository may set and no SDK rule enforces: operator placement (BRO1520), braces (`true`: BRO1514; `when_multiline`
removes no braces), using placement (BRO1008), arithmetic parentheses (`always_for_clarity`: BRO1406). Only a clear
form counts (the usual thresholds); a mixed result leaves the key in charge. A severity the repository sets for the rule
wins. Not included: brace and `else` placement (IDE0055 enforces them), `default` (IDE0034), and the private field style
from a naming rule (IDE1006).
