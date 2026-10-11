# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '&& BlankLineRuns.WantsBlankLineAfter(previous.Parent!, previous, items[i].GetFirstToken(), isOn, gapIsReplaced: true)'
    Replace = ''
    Tests = 'SingleLineBlocksTests'
}
