# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'
    Find = 'if (Line(text, lastItem.SpanStart) != openLine.LineNumber || GetOpening(last)'
    Replace = 'if (GetOpening(last)'
    Tests = 'ParenthesisPlacementTests'
}
