# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = '? [verb, "Gets"] : [verb]'
    Replace = '? [verb] : [verb]'
    Tests = 'DocumentationTests'
}
