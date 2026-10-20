# BRO1606: a filler sentence naming the type in <c> is replaced whole (trial 35, Castle Core)
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = '(?:instance|object)(?:\s+of\s+(?:the\s+)?(?:<see\s[^>]*/>|<c>[\w.<>{},]+</c>|'
    Replace = '(?:instance|object)(?:\s+of\s+(?:the\s+)?(?:<see\s[^>]*/>|'
    Tests = 'DocumentationTests.ConstructorSummaries_FillerWithTheNameInCode'
}
