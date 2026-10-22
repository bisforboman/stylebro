### Fixed

- `bro init` and `bro migrate` say when the root `.editorconfig` is a copy of a file in a restored NuGet package whose build
  copies it back before every build (NetEvolve.Defaults does): the settings they write would be gone after the next build.
  `bro format`, `bro check` and `bro preview` then say "Not verified" instead of "Clean".
- When xunit.analyzers 1.24 or later is restored (its fixes can't run under `dotnet format`), `bro format`, `bro check` and
  `bro preview` always say so, as a note that doesn't change the verdict: before, a fresh checkout said "Not verified" and an
  already restored one said "Clean".
