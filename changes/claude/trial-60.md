### Fixed

- **BRO1513** adds BRO1505's blank line too where a `#region` sits right below a member's `}` and above a doc comment:
  `bro format` no longer leaves a blank line above the `#region` or not depending on which rule it fixes first.
