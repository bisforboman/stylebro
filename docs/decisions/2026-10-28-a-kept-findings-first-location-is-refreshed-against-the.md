# A kept finding's first location is refreshed against the formatted text (2026-10-28)

## Question

Trial 59 (SharpYaml): a kept rename's "first:" location (`Kept: the name is in a string in the solution ... (first:
src/SharpYaml/Tokens/TagDirective.cs(52))`) is recorded when BRO1303's fix runs, before later fixes of the same `bro format`
change that file, and `bro check` replays the record. So it pointed to a line of the text before format: both engines named
`YamlSerializerContextGenerator.cs(1491)` where the formatted file has the string on line 1587 (BRO1514's braces), and on
three others the engines named different lines because their fix orders differ, the one difference in the trial's engine
soak. How should it be handled?

## Choices

- Fix it in this PR: the kept record's string is found again in the formatted text before format lists kept findings.
- Backlog it: note it in the trial, the soak shows the difference.

## Decision

"Fix it in this PR (Recommended)". When a "first:" line of a name-in-string kept finding no longer has a string with the
name, format moves it to the first such string in that file's text now, picked as the renamer picks it (a reflection-shaped
string with the name, else the string that is the name, else one with the name as a word). The name comes from the
finding's message, so the fixes' records stay as they are.
