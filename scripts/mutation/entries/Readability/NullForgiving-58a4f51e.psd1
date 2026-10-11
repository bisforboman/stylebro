# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = 'named => (named.ContainingType is null || IsFlat(named.ContainingType))'
    Replace = 'named => (true)'
    Tests = 'NullForgivingTests'
}
