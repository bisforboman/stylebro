### Changed

- **BRO1303/BRO1306** follow a naming rule (`dotnet_naming_rule`) for public, internal and protected fields only when
  something enforces it: IDE1006's severity, or the rule's own, is `suggestion`, `warning` or `error`. A rule nothing
  enforces (IDE1006 at `none`) leaves StyleCop's defaults. `stylebro-migrate init` likewise lets your code decide the
  private field style while IDE1006 is off.

### Fixed

- **`stylebro-migrate format --verify-no-changes`** no longer fails in a repository with an in-solution source
  generator (Mapperly) where `format` is clean: the generator now runs in the copy verify formats too.
