# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = '|| operand is DefaultExpressionSyntax'
    Replace = ''
    Tests = 'NullForgivingTests'
}
