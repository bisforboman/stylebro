# Options for declarations (2026-10-07): BRO1601 inheritdoc style, BRO1505 adjacent single-line members,
# BRO1105 / BRO1111 same_line
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'style.Trim() == "spaced"'
    Replace = 'style.Trim() != "spaced"'
    Tests = 'DocumentationTests'
}
