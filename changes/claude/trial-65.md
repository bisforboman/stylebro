### Fixed

- `bro check` no longer fails to load projects after `bro check --build` when a project's references depend on the configuration (project references in Debug, packages of the same names in Release): the next run restores again ("Not checked ... The target "CoreCompile" does not exist").
- In a repository CSharpier formats, a folder whose `.editorconfig` sets IDE0055 to `silent` is left out of the whitespace pass like one set to `none`; before, `bro format` and `bro check` said "Not clean: CSharpier puts back what the fixes changed" there with no way to settle it.
