### Fixed

- **bro format** run in a repository whose only solution is one folder down (`src/App.slnx`) now writes a new
  `stylebro.baseline` next to that solution, as `bro preview` and `bro format src/App.slnx` do, instead of at the root.
