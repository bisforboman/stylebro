# BRO1606: a filler sentence naming the type in <c> is replaced whole (trial 35, Castle Core)
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'var close = GetContainerEnd(rest);'
    Replace = 'var close = rest.IndexOf("</", StringComparison.Ordinal);'
    Tests = 'DocumentationTests.ConstructorSummaries_FillerWithTheNameInCode'
}
