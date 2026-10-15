# Trial 22: Gets or sets on get+init is fine.
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = '[verb, "Gets", "Gets or sets"]'
    Replace = '[verb, "Gets"]'
    Tests = 'DocumentationTests.InitAccessors_SayInitializes'
}
