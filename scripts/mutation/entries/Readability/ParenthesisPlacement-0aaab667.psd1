# Hardening: BRO1110 own_line reindents a ')' on its own line, like the line with '(' after the other fixes
@{
    File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'
    Find = 'return newText.Substring(lineBreak + 1);'
    Replace = 'break;'
    Tests = 'ParenthesisPlacementTests'
}
