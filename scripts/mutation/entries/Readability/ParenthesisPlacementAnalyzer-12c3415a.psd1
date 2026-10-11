# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacementAnalyzer.cs'
    Find = 'var openMoves = ownLine && '
    Replace = 'var openMoves = false && '
    Tests = 'ParenthesisPlacementTests'
}
