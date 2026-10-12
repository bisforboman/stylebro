# BRO1313 non_generic_methods: generic types whose type arguments differ in kind (IEnumerable<T> against IEnumerable<int>), like S927.
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = 'a.TypeArguments.Zip(e.TypeArguments, (x, y) => x.TypeKind != y.TypeKind).Any(different => different)'
    Replace = 'false'
    Tests = 'FullyQualifiedName~ParameterMatchesBaseTests'
}
