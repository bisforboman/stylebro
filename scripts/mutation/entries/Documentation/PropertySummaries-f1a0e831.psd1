# Trial 24 (Refit): StyleSharp reads init as Gets
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = '&& !(styleSharp && initOnly);'
    Replace = ';'
    Tests = 'DocumentationTests.PropertySummaries_FollowStyleSharp'
}
