# BRO1310 (Hungarian prefixes)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'declaration.ContainsDirectives ? TreeWalk.Trivia('
    Replace = 'false ? TreeWalk.Trivia('
    Tests = 'FieldNamingTests'
}
