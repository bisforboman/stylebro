# Trial 24 (Refit): SST1642 checks only the words before the type
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = '|| (styleSharp && accepted.Any(a => rest.StartsWith('
    Replace = '|| (false && accepted.Any(a => rest.StartsWith('
    Tests = 'DocumentationTests.ConstructorSummaries_FollowStyleSharp'
}
