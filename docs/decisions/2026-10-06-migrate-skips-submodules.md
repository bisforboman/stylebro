# Migrate skips submodules (2026-10-06)

## Question

A first run on Nerdbank.Streams: `stylebro-migrate --write` wrote a settings block into `ext/MessagePack/.editorconfig`
and added suppressions to 68 files there, all inside a git submodule (another repository's code). And the files a
project links from that submodule got StyleBro and SDK warnings (IDE0073, IDE0065, BRO1514, BRO1306) that
`stylebro-migrate format` never fixes, because it formats each project's own folder only. What should the tools do
with nested repositories, and how do users exclude vendored code?

## Choices

1. Skip every folder that is its own git repository (a `.git` file or folder below the root) in migrate, `init`,
   `format` and `baseline`; document `generated_code = true` for vendored folders and linked files.
2. Keep walking submodules and leave it to the user to revert those changes.
3. A `--exclude` option for migrate.

## Decision

The owner: choice 1, "docs + migrate skips submodules". Before, `--write` in a repository with a submodule at
`ext/MessagePack`:

```
 M .editorconfig
 M ext/MessagePack/.editorconfig                     <- another repository's file
 M ext/MessagePack/benchmark/.../Answer.cs           <- and 67 more
```

After: only the repository's own files change. Agent's choices: all four commands walk files through one function
(`StyleCopSetup.EnumerateFiles`), so the skip is there; `format` and `baseline` don't walk files themselves. Found on the
way: the linked files' warnings came from the root block turning rules on by id, which beats the
`dotnet_analyzer_diagnostic.severity = none` that `ext/.editorconfig` set for vendored code. Migrate now reads that bulk
key (it didn't before) and writes every replacing rule as `none` into such a folder, IDE0073 included. Verified
with dotnet format and the build that `generated_code = true` makes StyleBro, the SDK rules and the whitespace pass
skip a folder; getting-started.md documents it.
