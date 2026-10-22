# Trial 43: a one-word documentation text isn't a quote (too common).
@{
    File = 'src/StyleBro.CodeFixes/Documentation/QuotedDocumentation.cs'
    Find = 'text.IndexOf('' '') < 0'
    Replace = 'false'
    Tests = 'DocumentationTests.DocumentationText_TheSolutionQuotes_IsKept'
}
