# Trial 43: BRO1606's Fix All keeps quoted documentation text.
@{
    File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'
    Find = '                    && ConstructorSummaries.GetFinding(member, text, PropertySummaries.FollowsStyleSharp(options)) is { } finding
                    && !await IsQuotedAsync(document, root, finding.Replace.Start, diagnostic).ConfigureAwait(false))'
    Replace = '                    && ConstructorSummaries.GetFinding(member, text, PropertySummaries.FollowsStyleSharp(options)) is { } finding)'
    Tests = 'DocumentationTests.DocumentationText_TheSolutionQuotes_IsKept'
}
