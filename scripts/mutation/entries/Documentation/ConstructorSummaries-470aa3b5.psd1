# BRO1606: filler constructor sentences are replaced
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'var replaceLength = member is ConstructorDeclarationSyntax ? GetFillerLength(rest) : 0;'
    Replace = 'var replaceLength = 0;'
    Tests = 'DocumentationTests.ConstructorSummaries_Filler'
}
