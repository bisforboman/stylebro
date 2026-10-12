# BRO1115: nothing in crefs (no T? in a cref's type arguments).
@{
    File = 'src/StyleBro.Analyzers/Readability/NullableShorthand.cs'
    Find = ' || node.IsPartOfStructuredTrivia())'
    Replace = ')'
    Tests = 'FullyQualifiedName~CrefTests'
}
