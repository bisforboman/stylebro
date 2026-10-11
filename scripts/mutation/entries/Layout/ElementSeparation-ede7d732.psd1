# Options for declarations (2026-10-07): BRO1601 inheritdoc style, BRO1505 adjacent single-line members,
# BRO1105 / BRO1111 same_line
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'
    Find = '&& method.Body is null && !SpansSeveralLines(method, text),'
    Replace = '&& !SpansSeveralLines(method, text),'
    Tests = 'ElementSeparationTests'
}
