# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = '(not ? LanguageVersion.CSharp9 : LanguageVersion.CSharp7)'
    Replace = '(LanguageVersion.CSharp7)'
    Tests = 'NullCheckTests'
}
