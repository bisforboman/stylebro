# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = '(negated ? LanguageVersion.CSharp7 : LanguageVersion.CSharp9)'
    Replace = 'LanguageVersion.CSharp7'
    Tests = 'HasValueTests'
}
