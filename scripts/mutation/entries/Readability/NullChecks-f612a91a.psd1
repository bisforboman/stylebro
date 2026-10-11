# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = 'return op.MethodKind == MethodKind.BuiltinOperator'
    Replace = 'return true'
    Tests = 'NullCheckTests'
}
