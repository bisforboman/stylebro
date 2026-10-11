# Options for declarations (2026-10-07): BRO1601 inheritdoc style, BRO1505 adjacent single-line members,
# BRO1105 / BRO1111 same_line
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'
    Find = 'out var value) && value.Trim() == "true";'
    Replace = 'out var value);'
    Tests = 'MemberOrderingTests'
}
