# Doc rules skip a bare < (an element whose /> is missing)
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'documentation.DescendantTokens().Any(t => t.IsMissing)'
    Replace = 'false'
    Tests = 'DocumentationTests.MalformedDocumentation'
}
