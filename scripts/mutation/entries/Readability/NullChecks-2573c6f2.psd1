# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = 'type.TypeKind == TypeKind.Dynamic'
    Replace = 'false'
    Tests = 'NullCheckTests'
}
