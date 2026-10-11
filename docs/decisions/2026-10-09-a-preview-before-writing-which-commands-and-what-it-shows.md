# A preview before writing: which commands, and what it shows (2026-10-09)

## Question

Teams want to judge a migration or `init` before it touches their code. The backlog idea was a `--diff` that copies
the repository to a temporary folder, runs there and reports. Which commands get it, and what does it print?

```
Settings: 34 lines in .editorconfig (block), StyleBroPreset=none
First format run: 128 files, 412 changes, clean after 1 run
  BRO1001  member order        61 files
  BRO1514  braces              40 files
  IDE0055  whitespace          22 files
Sample (BRO1514, src/Lib/Parser.cs):
  -    if (x) return;
  +    if (x)
  +    {
  +        return;
  +    }
Full diff: stylebro-preview.patch (1,906 lines)
```

## Choices

1. Commands: only the migration (`stylebro-migrate <path> --diff`); the migration and `init`; all three (`format
   --diff` too).
2. Output: a console summary only; the full diff on the console; a console summary plus the full diff in a .patch file.

## Decision

The owner: all three commands, and a console summary (the example above) plus the full diff in a .patch file. Built
as `--diff[=file]` (default `stylebro-preview.patch` in the current folder) and `--keep`. The copy holds git's tracked
and untracked, not ignored files (submodules' working trees as repositories of their own, so format still skips them),
gets the StyleBro.Analyzers reference when the repository has none, and format runs until a run changes nothing (at
most 3). Rules come from a `--verify-no-changes --report` run before the fixes. On FFMpegCore the preview's patch for
`init --diff` was byte for byte what `init --write` plus two `stylebro-migrate format` runs wrote.
