# Trial 27: StyleCop compares the prefix ignoring case.
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 'return text.StartsWith(words, StringComparison.OrdinalIgnoreCase)'
    Replace = 'return text.StartsWith(words, StringComparison.Ordinal)'
    Tests = 'DocumentationTests.PropertySummaryWords_IgnoreCase'
}
