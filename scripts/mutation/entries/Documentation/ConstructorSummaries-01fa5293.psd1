@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'Normalize(a.TrimEnd(''.''))'
    Replace = 'a.TrimEnd(''.'')'
    Tests = 'DocumentationTests.StandardConstructorSummaries_AreNotReported'
}
