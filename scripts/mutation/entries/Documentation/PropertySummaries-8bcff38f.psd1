# Trial 24 (Refit): StyleSharp accepts a summary starting with the words and a space
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 'if (styleSharp && rest.StartsWith(verb + " ", StringComparison.Ordinal)'
    Replace = 'if (false && rest.StartsWith(verb + " ", StringComparison.Ordinal)'
    Tests = 'DocumentationTests.PropertySummaries_FollowStyleSharp'
}
