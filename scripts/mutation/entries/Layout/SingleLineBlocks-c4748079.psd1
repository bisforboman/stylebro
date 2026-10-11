# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'changes.AddRange(enclosing);'
    Replace = ''
    Tests = 'FixOrderTests'
}
