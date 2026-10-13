### Changed

- **BRO1303/BRO1306** follow a naming rule (`dotnet_naming_rule`) for public, internal and protected fields only when
  something enforces it: IDE1006's severity, or the rule's own, is `suggestion`, `warning` or `error`. A rule nothing
  enforces (IDE1006 at `none`) leaves StyleCop's defaults. `stylebro-migrate init` likewise lets your code decide the
  private field style while IDE1006 is off.
