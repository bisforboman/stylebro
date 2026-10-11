# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = '|| operand.HasTrailingTrivia'
    Replace = ''
    Tests = 'NullForgivingTests'
}
