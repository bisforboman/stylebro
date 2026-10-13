# Trial 18: BRO1603 adds no period after a quoted literal
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 'if (QuotedLiteral.IsMatch(word))'
    Replace = 'if (false)'
    Tests = 'DocumentationTests'
}
