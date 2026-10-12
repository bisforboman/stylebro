# BRO1303/BRO1306: a naming rule without a severity doesn't exist for Roslyn (YamlDotNet's readonly rule).
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'style is null || severity is null ||'
    Replace = 'style is null ||'
    Tests = 'FullyQualifiedName~FieldNamingTests'
}
