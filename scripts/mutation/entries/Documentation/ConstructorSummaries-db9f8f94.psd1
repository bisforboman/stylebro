# BRO1606: a constructor summary with text of its own isn't reported (#132)
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'if (member is ConstructorDeclarationSyntax && replaceLength == 0 && '
    Replace = 'if (false && '
    Tests = 'DocumentationTests.ConstructorSummaries_BeginWithTheStandardText'
}
