# Trial 30 covers unlisted projects with a spot repro (2026-10-17)

## Question

Trial 30's fourth kind of repository was a solution whose projects reference projects it doesn't list, ideally
multi-targeted, so that `format`'s "Leaving alone" path (`FormatCommand.OutsideProjectRuns`) runs on real code. About 60
unused public repositories were checked with a script, and none had a real case that also builds in the trial container:
dotnet/command-line-api needs a .NET 11 preview SDK, DryIoc targets net11.0, and the other hits were F# or VB projects.
What should replace that kind?

## Choices

- Spot repro: four repositories (Multiplayer, Chell, AvaloniaVisualBasic6, QuestPDF), plus a scratch copy of Chell whose
  solution no longer lists the multi-targeted library its projects reference. That copy is also used for `format --files`
  on a project outside the solution.
- SteveDunn/Vogen as a fifth repository. Its Consumers.sln doesn't list src/Vogen, but Vogen.sln at the root does, so
  it's only partly the case.
- Keep searching before starting.

## Decision

Spot repro (owner).
