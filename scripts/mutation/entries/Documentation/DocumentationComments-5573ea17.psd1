# Doc rules skip a bare < (an element whose /> is missing)
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'documentation.DescendantTokens().Any(t => t.IsMissing && !t.Parent!.AncestorsAndSelf().Any(n => n is XmlAttributeSyntax))'
    Replace = 'false'
    Tests = 'DocumentationTests.MalformedDocumentation'
}
