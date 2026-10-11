# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = '|| !IsSimpleOperand(operand)'
    Replace = ''
    Tests = 'NullCheckTests'
}
