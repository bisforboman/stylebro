### Fixed

- **BRO1309** no longer renames a static method to `Main` (`__Main` -> `Main`): any static `Main` can be an entry point, so
  the rename made a second one (CS0017) in projects that already have one, such as test projects on .NET Core.
- `bro preview` takes `--engine` (`bro preview init --engine host`, `bro preview format --engine host`, and
  `bro format --diff --engine ...`): before, `init` called it an unknown option, and `format` failed with "The preview
  failed". The preview's format runs now use that engine, as `bro format` does.
