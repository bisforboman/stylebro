# Trial 21: entities are well-formed
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = '&& !(token.IsKind(SyntaxKind.XmlEntityLiteralToken) && Entity.IsMatch(token.Text))'
    Replace = ''
    Tests = 'DocumentationTests.MalformedDocumentation_IsToldByTheEndTags'
}
