# BRO1604 skips malformed doc XML
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = '.Where(d => !DocumentationComments.IsMalformed(d))'
    Replace = ''
    Tests = 'FullyQualifiedName~DocumentationTests.MalformedDocumentation_IsLeftAlone'
}
