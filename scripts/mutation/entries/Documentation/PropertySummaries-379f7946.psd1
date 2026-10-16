# Trial 24 (Refit): StyleSharp counts a getter whatever its modifier
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 'var restrictedGetter = !styleSharp && '
    Replace = 'var restrictedGetter = '
    Tests = 'DocumentationTests.PropertySummaries_FollowStyleSharp'
}
