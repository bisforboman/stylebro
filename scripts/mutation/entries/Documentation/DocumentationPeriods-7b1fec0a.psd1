# BRO1603: elements StyleCop doesn't know are passed over
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 'if (InlineElements.Contains(name))'
    Replace = 'if (true)'
    Tests = 'DocumentationTests.UnknownElements_ArePassedOver'
}
