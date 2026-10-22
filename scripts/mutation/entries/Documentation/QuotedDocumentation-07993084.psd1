# Trial 43: documentation text a string in the solution quotes is kept.
@{
    File = 'src/StyleBro.CodeFixes/Documentation/QuotedDocumentation.cs'
    Find = 'if (names.Strings.Contains(text))'
    Replace = 'if (false)'
    Tests = 'DocumentationTests.DocumentationText_TheSolutionQuotes_IsKept'
}
