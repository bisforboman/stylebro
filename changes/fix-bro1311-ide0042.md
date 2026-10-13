### Fixed

- **BRO1311** and the renaming rules (BRO1301-BRO1309) no longer break the build where IDE0042 (deconstruct variable
  declaration) is a warning or an error: a rename that would end a clash between a tuple element and a variable, so that
  IDE0042 reports the tuple's variable, is left with its warning.
