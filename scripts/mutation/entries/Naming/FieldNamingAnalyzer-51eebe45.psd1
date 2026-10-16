# BRO1306: a field of a private type is skipped only for disabled code in its scope (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'
    Find = 'scope = type.ContainingType;'
    Replace = ''
    Tests = 'FieldNamingTests.FieldsOfAPrivateType_OnlyTheirScopesDisabledCodeCounts'
}
