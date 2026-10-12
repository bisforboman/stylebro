# BRO1607: a finalizer summary still gets the standard sentence in front (#132)
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'member is ConstructorDeclarationSyntax && replaceLength == 0 && !rest'
    Replace = 'replaceLength == 0 && !rest'
    Tests = 'DocumentationTests.ConstructorSummaries_BeginWithTheStandardText'
}
