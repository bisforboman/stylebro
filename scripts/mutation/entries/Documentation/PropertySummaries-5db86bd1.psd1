# BRO1604 skips malformed doc XML
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 'found && !DocumentationComments.IsMalformed(d) ? found'
    Replace = 'found ? found'
    Tests = 'DocumentationTests.MalformedDocumentation_IsLeftAlone'
}
