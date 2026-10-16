# Trial 24 (Refit): stylebro_summary_wording = stylesharp
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = '.Equals("stylesharp", StringComparison.OrdinalIgnoreCase)'
    Replace = '.Equals("stylecop", StringComparison.OrdinalIgnoreCase)'
    Tests = 'DocumentationTests.PropertySummaries_FollowStyleSharp'
}
