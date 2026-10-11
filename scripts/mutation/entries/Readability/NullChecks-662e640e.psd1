# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = '&& symbol is IMethodSymbol { MethodKind: not MethodKind.BuiltinOperator }'
    Replace = '&& false'
    Tests = 'NullCheckTests'
}
