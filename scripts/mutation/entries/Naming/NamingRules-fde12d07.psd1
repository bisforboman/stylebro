# trial 49: ReSharper's InconsistentNaming suppressions keep names
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = '|| IsDisabledByReSharper(tree, location.SourceSpan.Start, cancellationToken)'
    Replace = ''
    Tests = 'CamelCaseNamingTests.NamesWhereReSharpersInconsistentNamingIsSuppressed_KeepTheirNames'
}
