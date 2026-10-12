### Changed

- **BRO1514-BRO1516** leave `fixed (a) fixed (b) { }` alone: consecutive `fixed` statements share one block, like
  consecutive `using`s (and like the SDK's IDE0011). `stylebro_allow_consecutive_usings = false` nests them again.
