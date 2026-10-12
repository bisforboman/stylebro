# BRO1604: a bool summary that starts with an article isn't reported (#132)
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 'if (old.Length == 0 && isBool && StartsWithAnyWord(rest, Articles))'
    Replace = 'if (false)'
    Tests = 'DocumentationTests.PropertySummaries_MatchTheAccessors'
}
