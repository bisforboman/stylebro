# Public-API listing snapshots hold only public names (2026-10-21)

## Question

Trial 38, a spot copy of AwesomeAssertions with private fields switched to `_camelCase`: 127 private-field renames were kept
because their names (`assertionChain`, `subject`, `predicate`, ...) appear in `Tests/Approval.Tests/ApprovedApi/*.verified.txt`,
the PublicApiGenerator listing of the public API, where they are only public parameter names. The listing never holds
private members; the other renames left all tests passing. How should the snapshot guard (widened in trial 37) treat such
listings?

## Choices

1. **API listings: public names only** (owner's pick): a snapshot shaped like a PublicApiGenerator/ApiApprover listing (a
   `namespace X` line, then `public` type declarations) holds only names visible outside the assembly; private and internal
   members, locals and parameters of non-public members rename normally. Other snapshot files keep holding every name
   (trial 37's Shouldly case).
2. Keep holding every name (127 kept and baselined).
3. A private field's name in any snapshot never holds it unless it looks like a field name (`_x`, `m_x`).

## Decision

API listings: public names only. `SnapshotFiles.IsApiListing` sends such a file's words to `Words.ApiListings`;
`CamelCaseRenamer.FindQuoted` keeps a name found there only when `IsInPublicApi` (the symbol and every type around it public
or protected, a parameter by its member). Such names are renamed only with `stylebro_rename_public_api = true`.
