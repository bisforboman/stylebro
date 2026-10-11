# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'
    Find = '? (true, openLine) : (false, null);'
    Replace = '? (false, openLine) : (false, null);'
    Tests = 'ParenthesisPlacementTests'
}
