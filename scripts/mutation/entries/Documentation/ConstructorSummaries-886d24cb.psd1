# BRO1606: an empty constructor summary still gets the standard sentence (#132)
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = ' && !rest.StartsWith("</", StringComparison.Ordinal))'
    Replace = ')'
    Tests = 'DocumentationTests.ConstructorSummaries_BeginWithTheStandardText'
}
