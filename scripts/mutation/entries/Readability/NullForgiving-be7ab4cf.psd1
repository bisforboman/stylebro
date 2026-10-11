# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = 'return IsFlat(model.GetTypeInfo(operand, cancellationToken).Type)'
    Replace = 'return true'
    Tests = 'NullForgivingTests'
}
