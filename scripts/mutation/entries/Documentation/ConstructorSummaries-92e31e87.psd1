@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'string.Join(", ", typeParameters'
    Replace = 'string.Join(",", typeParameters'
    Tests = 'DocumentationTests.ConstructorSummaries_GenericLinkHasSpaces'
}
