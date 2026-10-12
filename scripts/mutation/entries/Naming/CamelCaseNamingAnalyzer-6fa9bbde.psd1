# BRO1313 non_generic_methods: a base parameter whose type is a type parameter the member fills in keeps the member's name (S927).
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '(expected.TypeKind == TypeKind.TypeParameter && actual.TypeKind != TypeKind.TypeParameter)'
    Replace = 'false'
    Tests = 'ParameterMatchesBaseTests'
}
