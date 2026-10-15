# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = '? [verb, "Gets", "Gets or sets"] : [verb]'
    Replace = '? [verb] : [verb]'
    Tests = 'DocumentationTests'
}
