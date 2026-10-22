### Fixed

- `bro init` and `bro migrate` say when the root `.editorconfig` is a copy of a file in a restored NuGet package whose build
  copies it back before every build (NetEvolve.Defaults does): the settings they write would be gone after the next build.
  `bro format`, `bro check` and `bro preview` then say "Not verified" instead of "Clean".
