# Trial 50 tries a runner/fixer split (2026-10-23)

## Question

A trial session has so far done everything itself: picked and cloned the repositories, ran both engines, built and
tested, then fixed StyleBro and re-ran. The repositories' runs are independent and slow, and one session's context
fills with their logs. Should a trial split that work?

## Choices

1. **Runners and a fixer** (owner's pick, "Try it on trial 50"): one cloud session per repository runs it (clone,
   measure, both engines, build, test) and changes no StyleBro code; each writes
   `.trials/trial50/findings/<owner>--<name>.md` to the trial branch. One fixer session picks the repositories,
   reproduces each finding at unit-test or small-repro level without cloning the repositories, fixes everything in one
   PR, and asks the runners to re-run on its commit.
2. Keep one session per trial.

## Decision

Try the split on trial 50; the trial's notes in docs/notes/trials.md say how it went.
