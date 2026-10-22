# Trial 43: BRO1603's Fix All keeps quoted documentation text.
@{
    File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'
    Find = 'if (missingPeriods.Contains(span.Start) && !await IsQuotedAsync(document, root, span.Start, diagnostic).ConfigureAwait(false))'
    Replace = 'if (missingPeriods.Contains(span.Start))'
    Tests = 'DocumentationTests.DocumentationText_TheSolutionQuotes_IsKept'
}
