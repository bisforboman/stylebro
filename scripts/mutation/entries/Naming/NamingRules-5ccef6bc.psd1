# trial 49: 'disable once' covers one declaration, not the rest of the file
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = 'match.Success && match.Groups["once"].Success == once'
    Replace = 'match.Success'
    Tests = 'CamelCaseNamingTests.NamesWhereReSharpersInconsistentNamingIsSuppressed_KeepTheirNames'
}
