# Trial 24 (Refit): StyleSharp counts any setter modifier as restricted
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = '(styleSharp ? setter.Modifiers.Count > 0 : IsRestricted(setter))'
    Replace = 'IsRestricted(setter)'
    Tests = 'DocumentationTests.PropertySummaries_FollowStyleSharp'
}
