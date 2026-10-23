### Fixed

- After you turn off a NuGet package's copying of its `.editorconfig` over the root one (NetEvolve.Defaults'
  `DisableSupportAdditionalFiles`), `bro check --build` notices that its builds leave the file as it is and says "Clean";
  `bro init`, `bro format`, `bro check` and `bro preview` stop naming the package from then on (until a project or build
  file changes). Before, they kept saying "Not verified" although the builds read StyleBro's settings.
