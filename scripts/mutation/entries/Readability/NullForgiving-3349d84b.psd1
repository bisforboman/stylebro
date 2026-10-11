# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = '&& named.TypeArguments.All(a => a.IsValueType && IsFlat(a))'
    Replace = ''
    Tests = 'NullForgivingTests'
}
