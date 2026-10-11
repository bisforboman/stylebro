# BRO1606: constructor sentences of their own (Instantiates, Constructs, Creates a new)
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = '|| (member is ConstructorDeclarationSyntax && ConstructorVerbs.Any(v => StartsWithWords(remaining, v)))))'
    Replace = '))'
    Tests = 'DocumentationTests.SummariesThatCantFollowTheStandardWords_AreNotReported'
}
