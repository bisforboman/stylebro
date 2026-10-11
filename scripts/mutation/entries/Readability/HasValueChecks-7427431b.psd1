# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = '|| !NullChecks.IsLooseParent(judged)'
    Replace = ''
    Tests = 'HasValueTests'
}
