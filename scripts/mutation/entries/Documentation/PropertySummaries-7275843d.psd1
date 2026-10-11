# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = ' || old.StartsWith("Gets or initializes", StringComparison.Ordinal)'
    Replace = ''
    Tests = 'DocumentationTests'
}
