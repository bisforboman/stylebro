# Trial 24 (Refit): SST1624 rejects Gets or sets for a restricted setter
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = '&& !(restrictedSetter && rest.StartsWith("Gets or sets ", StringComparison.Ordinal)))'
    Replace = ')'
    Tests = 'DocumentationTests.PropertySummaries_FollowStyleSharp'
}
